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
    public class PersistedGrantStore : PersistedGrantStore<Entities.PersistedGrant>
    {
        public PersistedGrantStore(
            IdentityServerDocumentSessionProvider identityServerDocumentSessionProvider,
            IIdentityServerStoreMapper mapper,
            ILogger<PersistedGrantStore<Entities.PersistedGrant>> logger,
            IOptionsSnapshot<OperationalStoreOptions> operationalStoreOptions)
            : base(identityServerDocumentSessionProvider, mapper, logger, operationalStoreOptions)
        {
        }
    }

    public abstract class PersistedGrantStore<TPersistedGrantEntity> : IPersistedGrantStore
        where TPersistedGrantEntity : Entities.PersistedGrant
    {
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

        protected IAsyncDocumentSession DocumentSession { get; }

        protected IIdentityServerStoreMapper Mapper { get; }

        protected ILogger<PersistedGrantStore<TPersistedGrantEntity>> Logger { get; }

        protected IOptionsSnapshot<OperationalStoreOptions> OperationalStoreOptions { get; }

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

        protected virtual async Task StoreAsync(TPersistedGrantEntity grantEntity)
        {
            if (!CheckRequiredFields(grantEntity, out string errorMsg))
            {
                Logger.LogError($"Error storing persisted grant because of required fields check failure: {errorMsg}");
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
                Logger.LogError($"Error storing persisted grant with error: {operationResult.Error}");
            }
        }

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
                    "Error deleting persisted grant {}. {}.",
                    entityId,
                    ErrorDescriber.ConcurrencyException
                );
            }
            catch (Exception ex)
            {
                Logger.LogError(
                    ex,
                    "Error deleting persisted grant {}. {}.",
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
                    "Remove all persisted grants operation took more than {} seconds.",
                    deleteOperationTimeoutSec
                );
            }
        }

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
                    "Error creating persisted grant. Key {0}; Entity ID {1}",
                    entity.Key,
                    entity.Id
                );
                return StoreResult.Failure(ErrorDescriber.ConcurrencyException);
            }
            catch (Exception ex)
            {
                Logger.LogError(
                    ex,
                    "Error creating persisted grant. Key {0}; Entity ID {1}",
                    entity.Key,
                    entity.Id
                );
                return StoreResult.Failure(ErrorDescriber.GeneralError);
            }
        }

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
                    "Error updating persisted grant. Entity ID {1}",
                    entityId
                );
                return StoreResult.Failure(ErrorDescriber.ConcurrencyException);
            }
            catch (Exception ex)
            {
                Logger.LogError(
                    ex,
                    "Error updating persisted grant. Entity ID {1}",
                    entityId
                );
                return StoreResult.Failure(ErrorDescriber.GeneralError);
            }
        }

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