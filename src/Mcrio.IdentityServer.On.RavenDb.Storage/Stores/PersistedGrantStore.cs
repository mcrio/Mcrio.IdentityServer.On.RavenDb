using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using IdentityServer4.Extensions;
using IdentityServer4.Models;
using IdentityServer4.Stores;
using Mcrio.IdentityServer.On.RavenDb.Storage.Mappers;
using Mcrio.IdentityServer.On.RavenDb.Storage.RavenDb;
using Mcrio.IdentityServer.On.RavenDb.Storage.Stores.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Raven.Client.Documents;
using Raven.Client.Documents.Commands;
using Raven.Client.Documents.Linq;
using Raven.Client.Documents.Operations;
using Raven.Client.Documents.Session;
using Raven.Client.Exceptions;

namespace Mcrio.IdentityServer.On.RavenDb.Storage.Stores
{
    /// <inheritdoc />
    public class PersistedGrantStore : PersistedGrantStore<Entities.PersistedGrant>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PersistedGrantStore"/> class.
        /// </summary>
        /// <param name="identityServerDocumentSessionProvider"></param>
        /// <param name="mapper"></param>
        /// <param name="logger"></param>
        /// <param name="operationalStoreOptions"></param>
        public PersistedGrantStore(
            IdentityServerDocumentSessionProvider identityServerDocumentSessionProvider,
            IIdentityServerStoreMapper mapper,
            ILogger<PersistedGrantStore> logger,
            IOptionsSnapshot<OperationalStoreOptions> operationalStoreOptions)
            : base(identityServerDocumentSessionProvider, mapper, logger, operationalStoreOptions)
        {
        }
    }

    /// <inheritdoc />
    public abstract class PersistedGrantStore<TPersistedGrantEntity> : IPersistedGrantStore
        where TPersistedGrantEntity : Entities.PersistedGrant
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PersistedGrantStore{TPersistedGrantEntity}"/> class.
        /// </summary>
        /// <param name="identityServerDocumentSessionProvider"></param>
        /// <param name="mapper"></param>
        /// <param name="logger"></param>
        /// <param name="operationalStoreOptions"></param>
        protected PersistedGrantStore(
            IdentityServerDocumentSessionProvider identityServerDocumentSessionProvider,
            IIdentityServerStoreMapper mapper,
            ILogger<PersistedGrantStore<TPersistedGrantEntity>> logger,
            IOptionsSnapshot<OperationalStoreOptions> operationalStoreOptions)
        {
            DocumentSession = identityServerDocumentSessionProvider();
            Mapper = mapper;
            Logger = logger;
            OperationalStoreOptions = operationalStoreOptions;
        }

        /// <summary>
        /// Gets the document session.
        /// </summary>
        protected IAsyncDocumentSession DocumentSession { get; }

        /// <summary>
        /// Gets the mapper.
        /// </summary>
        protected IIdentityServerStoreMapper Mapper { get; }

        /// <summary>
        /// Gets the logger.
        /// </summary>
        protected ILogger<PersistedGrantStore<TPersistedGrantEntity>> Logger { get; }

        /// <summary>
        /// Gets the operational store options.
        /// </summary>
        protected IOptionsSnapshot<OperationalStoreOptions> OperationalStoreOptions { get; }

        /// <inheritdoc />
        public virtual Task StoreAsync(PersistedGrant grant)
        {
            if (grant == null)
            {
                throw new ArgumentNullException(nameof(grant));
            }

            TPersistedGrantEntity grantEntity =
                Mapper.ToEntity<PersistedGrant, TPersistedGrantEntity>(grant);

            return StoreAsync(grantEntity);
        }

        /// <inheritdoc />
        public virtual async Task<PersistedGrant> GetAsync(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentNullException(nameof(key));
            }

            string entityId = Mapper.CreateEntityId<TPersistedGrantEntity>(key);
            TPersistedGrantEntity entity = await DocumentSession
                .LoadAsync<TPersistedGrantEntity>(entityId)
                .ConfigureAwait(false);

            return entity is null
                ? null!
                : Mapper.ToModel<TPersistedGrantEntity, PersistedGrant>(entity);
        }

        /// <inheritdoc />
        public virtual async Task<IEnumerable<PersistedGrant>> GetAllAsync(
            PersistedGrantFilter filter)
        {
            filter.Validate();

            IRavenQueryable<TPersistedGrantEntity> query = DocumentSession.Query<TPersistedGrantEntity>();
            query = ApplyFilter(query, filter);

            var grants = new List<PersistedGrant>();

            IAsyncEnumerator<StreamResult<TPersistedGrantEntity>>? grantStreamResult =
                await DocumentSession.Advanced
                    .StreamAsync(query)
                    .ConfigureAwait(false);
            while (await grantStreamResult.MoveNextAsync().ConfigureAwait(false))
            {
                grants.Add(Mapper.ToModel<TPersistedGrantEntity, PersistedGrant>(
                    grantStreamResult.Current.Document));
            }

            return grants;
        }

        /// <inheritdoc />
        public virtual async Task RemoveAsync(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentNullException(nameof(key));
            }

            string entityId = Mapper.CreateEntityId<TPersistedGrantEntity>(key);

            try
            {
                DocumentSession.Delete(entityId);
                await DocumentSession.SaveChangesAsync().ConfigureAwait(false);
            }
            catch (ConcurrencyException concurrencyException)
            {
                Logger.LogError(
                    concurrencyException,
                    "Error deleting persisted grant {EntityId}. {Exception}",
                    entityId,
                    ErrorDescriber.ConcurrencyException
                );
            }
            catch (Exception ex)
            {
                Logger.LogError(
                    ex,
                    "Error deleting persisted grant {EntityId}. {Error}",
                    entityId,
                    ErrorDescriber.GeneralError
                );
            }
        }

        /// <inheritdoc/>
        public virtual async Task RemoveAllAsync(PersistedGrantFilter filter)
        {
            filter.Validate();

            IRavenQueryable<TPersistedGrantEntity> query = DocumentSession.Query<TPersistedGrantEntity>();
            query = ApplyFilter(query, filter);

            const int deleteOperationTimeoutSec = 30;
            try
            {
                // todo: do we need to throttle this operation?
                Operation operation = await DocumentSession.Advanced.DocumentStore.Operations.SendAsync(
                    new DeleteByQueryOperation(query.ToAsyncDocumentQuery().GetIndexQuery())
                ).ConfigureAwait(false);
                await operation.WaitForCompletionAsync(TimeSpan.FromSeconds(deleteOperationTimeoutSec));
            }
            catch (TimeoutException)
            {
                Logger.LogWarning(
                    "Remove all persisted grants operation took more than {Seconds} seconds",
                    deleteOperationTimeoutSec
                );
            }
        }

        /// <summary>
        /// Store persisted grant entity.
        /// </summary>
        /// <param name="grantEntity">Grant entity.</param>
        /// <returns>The <see cref="Task"/> that represents the asynchronous operation.</returns>
        protected virtual async Task StoreAsync(TPersistedGrantEntity grantEntity)
        {
            if (!CheckRequiredFields(grantEntity, out string errorMsg))
            {
                Logger.LogError(
                    "Error storing persisted grant because of required fields check failure: {ErrorMsg}",
                    errorMsg
                );
                return;
            }

            string entityId = grantEntity.Id;
            TPersistedGrantEntity entityInSession = await DocumentSession
                .LoadAsync<TPersistedGrantEntity>(entityId)
                .ConfigureAwait(false);

            StoreResult operationResult;
            if (entityInSession is null)
            {
                operationResult = await CreateAsync(grantEntity).ConfigureAwait(false);
            }
            else
            {
                operationResult = await UpdateAsync(grantEntity, entityInSession).ConfigureAwait(false);
            }

            if (operationResult.IsFailure)
            {
                Logger.LogError(
                    "Error storing persisted grant with error: {Error}",
                    operationResult.Error
                );
            }
        }

        /// <summary>
        /// Make sure Persisted Grant entity required fields are populated.
        /// </summary>
        /// <param name="persistedGrant">Persisted grant.</param>
        /// <param name="errorMessage">Error message.</param>
        /// <returns>TRUE if required fields are populated, FALSE otherwise.</returns>
        protected virtual bool CheckRequiredFields(TPersistedGrantEntity persistedGrant, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(persistedGrant.Key))
            {
                errorMessage = ErrorDescriber.PersistedGrantKeyMissing;
                return false;
            }

            if (string.IsNullOrWhiteSpace(persistedGrant.Type))
            {
                errorMessage = ErrorDescriber.PersistedGrantTypeMissing;
                return false;
            }

            if (string.IsNullOrWhiteSpace(persistedGrant.ClientId))
            {
                errorMessage = ErrorDescriber.PersistedGrantClientIdMissing;
                return false;
            }

            if (persistedGrant.CreationTime == default)
            {
                errorMessage = ErrorDescriber.PersistedGrantCreationTimeMissing;
                return false;
            }

            if (string.IsNullOrWhiteSpace(persistedGrant.Data))
            {
                errorMessage = ErrorDescriber.PersistedGrantDataMissing;
                return false;
            }

            if (string.IsNullOrWhiteSpace(persistedGrant.Id))
            {
                errorMessage = ErrorDescriber.IdMustNotBeEmpty;
                return false;
            }

            return true;
        }

        /// <summary>
        /// Create persisted grant entity.
        /// </summary>
        /// <param name="entity">Entity.</param>
        /// <returns>The <see cref="Task"/> that represents the asynchronous operation.</returns>
        protected virtual async Task<StoreResult> CreateAsync(TPersistedGrantEntity entity)
        {
            try
            {
                await DocumentSession.StoreAsync(entity, string.Empty, entity.Id).ConfigureAwait(false);
                DocumentSession.ManageDocumentExpiresMetadata(
                    OperationalStoreOptions.Value,
                    entity,
                    entity.Expiration
                );
                await DocumentSession.SaveChangesAsync().ConfigureAwait(false);
                return StoreResult.Success();
            }
            catch (ConcurrencyException concurrencyException)
            {
                Logger.LogError(
                    concurrencyException,
                    "Error creating persisted grant. Key {EntityKey}; Entity ID {EntityId}",
                    entity.Key,
                    entity.Id
                );
                return StoreResult.Failure(ErrorDescriber.ConcurrencyException);
            }
            catch (Exception ex)
            {
                Logger.LogError(
                    ex,
                    "Error creating persisted grant. Key {EntityKey}; Entity ID {EntityId}",
                    entity.Key,
                    entity.Id
                );
                return StoreResult.Failure(ErrorDescriber.GeneralError);
            }
        }

        /// <summary>
        /// Update persisted grant.
        /// </summary>
        /// <param name="newGrantData">New persisted grant.</param>
        /// <param name="entityInSession">Persisted grant loaded in session.</param>
        /// <returns>The <see cref="Task"/> that represents the asynchronous operation.</returns>
        protected virtual async Task<StoreResult> UpdateAsync(
            TPersistedGrantEntity newGrantData,
            TPersistedGrantEntity entityInSession)
        {
            Mapper.Map(newGrantData, entityInSession);
            string entityId = entityInSession.Id;

            try
            {
                string changeVector = DocumentSession.Advanced.GetChangeVectorFor(entityInSession);
                await DocumentSession
                    .StoreAsync(entityInSession, changeVector, entityId)
                    .ConfigureAwait(false);
                DocumentSession.ManageDocumentExpiresMetadata(
                    OperationalStoreOptions.Value,
                    entityInSession,
                    entityInSession.Expiration
                );
                await DocumentSession.SaveChangesAsync().ConfigureAwait(false);
                return StoreResult.Success();
            }
            catch (ConcurrencyException concurrencyException)
            {
                Logger.LogError(
                    concurrencyException,
                    "Error updating persisted grant. Entity ID {EntityId}",
                    entityId
                );
                return StoreResult.Failure(ErrorDescriber.ConcurrencyException);
            }
            catch (Exception ex)
            {
                Logger.LogError(
                    ex,
                    "Error updating persisted grant. Entity ID {EntityId}",
                    entityId
                );
                return StoreResult.Failure(ErrorDescriber.GeneralError);
            }
        }

        /// <summary>
        /// Apply search filter.
        /// </summary>
        /// <param name="query">Query.</param>
        /// <param name="filter">Filter to apply to query.</param>
        /// <returns>Provided Raven queryable object.</returns>
        protected virtual IRavenQueryable<TPersistedGrantEntity> ApplyFilter(
            IRavenQueryable<TPersistedGrantEntity> query,
            PersistedGrantFilter filter)
        {
            if (!string.IsNullOrWhiteSpace(filter.ClientId))
            {
                query = query.Where(x => x.ClientId == filter.ClientId);
            }

            if (!string.IsNullOrWhiteSpace(filter.SessionId))
            {
                query = query.Where(x => x.SessionId == filter.SessionId);
            }

            if (!string.IsNullOrWhiteSpace(filter.SubjectId))
            {
                query = query.Where(x => x.SubjectId == filter.SubjectId);
            }

            if (!string.IsNullOrWhiteSpace(filter.Type))
            {
                query = query.Where(x => x.Type == filter.Type);
            }

            return query;
        }
    }
}