using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using IdentityServer4.Extensions;
using IdentityServer4.Models;
using IdentityServer4.Stores;
using Mcrio.IdentityServer.On.RavenDb.Storage.Mappers;
using Mcrio.IdentityServer.On.RavenDb.Storage.Stores.Additions;
using Mcrio.IdentityServer.On.RavenDb.Storage.Stores.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Raven.Client.Documents;
using Raven.Client.Documents.Linq;
using Raven.Client.Documents.Operations;
using Raven.Client.Documents.Session;
using Raven.Client.Exceptions;

namespace Mcrio.IdentityServer.On.RavenDb.Storage.Stores
{
    public class PersistedGrantStore : IPersistedGrantStore
    {
        private readonly IAsyncDocumentSession _documentSession;
        private readonly IIdentityServerStoreMapper _mapper;
        private readonly ILogger<PersistedGrantStore> _logger;
        private readonly IOptionsSnapshot<OperationalStoreOptions> _operationalStoreOptions;

        public PersistedGrantStore(
            IdentityServerDocumentSessionProvider identityServerDocumentSessionProvider,
            IIdentityServerStoreMapper mapper,
            ILogger<PersistedGrantStore> logger,
            IOptionsSnapshot<OperationalStoreOptions> operationalStoreOptions)
        {
            _documentSession = identityServerDocumentSessionProvider();
            _mapper = mapper;
            _logger = logger;
            _operationalStoreOptions = operationalStoreOptions;
        }

        public async Task StoreAsync(PersistedGrant grant)
        {
            if (grant == null)
            {
                throw new ArgumentNullException(nameof(grant));
            }

            Entities.PersistedGrant grantEntity = _mapper.ToEntity(grant);
            if (!CheckRequiredFields(grantEntity, out string errorMsg))
            {
                _logger.LogError($"Error storing persisted grant because of required fields check failure: {errorMsg}");
                return;
            }

            string entityId = grantEntity.Id;
            Entities.PersistedGrant entityInSession = await _documentSession
                .LoadAsync<Entities.PersistedGrant>(entityId)
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
                _logger.LogError($"Error storing persisted grant with error: {operationResult.Error}");
            }
        }

        public async Task<PersistedGrant> GetAsync(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentNullException(nameof(key));
            }

            string entityId = _mapper.CreateEntityId<Entities.PersistedGrant>(key);
            Entities.PersistedGrant entity = await _documentSession
                .LoadAsync<Entities.PersistedGrant>(entityId)
                .ConfigureAwait(false);

            return entity is null ? null! : _mapper.ToModel(entity);
        }

        public async Task<IEnumerable<PersistedGrant>> GetAllAsync(PersistedGrantFilter filter)
        {
            filter.Validate();

            IRavenQueryable<Entities.PersistedGrant> query = _documentSession.Query<Entities.PersistedGrant>();
            query = ApplyFilter(query, filter);

            var grants = new List<PersistedGrant>();

            var grantStreamResult = await _documentSession.Advanced.StreamAsync(query).ConfigureAwait(false);
            while (await grantStreamResult.MoveNextAsync().ConfigureAwait(false))
            {
                grants.Add(_mapper.ToModel(grantStreamResult.Current.Document));
            }

            return grants;
        }

        /// <inheritdoc />
        public async Task RemoveAsync(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentNullException(nameof(key));
            }

            string entityId = _mapper.CreateEntityId<Entities.PersistedGrant>(key);

            Entities.PersistedGrant entityInSession = await _documentSession
                .LoadAsync<Entities.PersistedGrant>(entityId)
                .ConfigureAwait(false);

            if (entityInSession is null)
            {
                _logger.LogWarning(
                    "Error removing persisted token. {}",
                    string.Format(ErrorDescriber.EntityNotFound, entityId)
                );
            }

            try
            {
                string changeVector = _documentSession.Advanced.GetChangeVectorFor(entityInSession);
                _documentSession.Delete(entityId, changeVector);
                await _documentSession.SaveChangesAsync().ConfigureAwait(false);
            }
            catch (ConcurrencyException concurrencyException)
            {
                _logger.LogError(
                    concurrencyException,
                    "Error deleting persisted grant {}. {}.",
                    entityId,
                    ErrorDescriber.ConcurrencyException
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error deleting persisted grant {}. {}.",
                    entityId,
                    ErrorDescriber.GeneralError
                );
            }
        }

        /// <inheritdoc/>
        public async Task RemoveAllAsync(PersistedGrantFilter filter)
        {
            filter.Validate();

            IRavenQueryable<Entities.PersistedGrant> query = _documentSession.Query<Entities.PersistedGrant>();
            query = ApplyFilter(query, filter);

            const int deleteOperationTimeoutSec = 30;
            try
            {
                // todo: do we need to throttle this operation?
                Operation operation = await _documentSession.Advanced.DocumentStore.Operations.SendAsync(
                    new DeleteByQueryOperation(query.ToAsyncDocumentQuery().GetIndexQuery())
                ).ConfigureAwait(false);
                await operation.WaitForCompletionAsync(TimeSpan.FromSeconds(deleteOperationTimeoutSec));
            }
            catch (TimeoutException)
            {
                _logger.LogWarning(
                    "Remove all persisted grants operation took more than {} seconds.",
                    deleteOperationTimeoutSec
                );
            }
        }

        protected virtual bool CheckRequiredFields(Entities.PersistedGrant persistedGrant, out string errorMessage)
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

        protected virtual async Task<StoreResult> CreateAsync(Entities.PersistedGrant entity)
        {
            try
            {
                await _documentSession.StoreAsync(entity, string.Empty, entity.Id).ConfigureAwait(false);
                _documentSession.ManageDocumentExpiresMetadata(
                    _operationalStoreOptions.Value,
                    entity,
                    entity.Expiration
                );
                await _documentSession.SaveChangesAsync().ConfigureAwait(false);
                return StoreResult.Success();
            }
            catch (ConcurrencyException concurrencyException)
            {
                _logger.LogError(
                    concurrencyException,
                    "Error creating persisted grant. Key {0}; Entity ID {1}",
                    entity.Key,
                    entity.Id
                );
                return StoreResult.Failure(ErrorDescriber.ConcurrencyException);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error creating persisted grant. Key {0}; Entity ID {1}",
                    entity.Key,
                    entity.Id
                );
                return StoreResult.Failure(ErrorDescriber.GeneralError);
            }
        }

        protected virtual async Task<StoreResult> UpdateAsync(
            Entities.PersistedGrant newGrantData,
            Entities.PersistedGrant entityInSession)
        {
            _mapper.Map(newGrantData, entityInSession);
            string entityId = entityInSession.Id;

            try
            {
                string changeVector = _documentSession.Advanced.GetChangeVectorFor(entityInSession);
                await _documentSession
                    .StoreAsync(entityInSession, changeVector, entityId)
                    .ConfigureAwait(false);
                _documentSession.ManageDocumentExpiresMetadata(
                    _operationalStoreOptions.Value,
                    entityInSession,
                    entityInSession.Expiration
                );
                await _documentSession.SaveChangesAsync().ConfigureAwait(false);
                return StoreResult.Success();
            }
            catch (ConcurrencyException concurrencyException)
            {
                _logger.LogError(
                    concurrencyException,
                    "Error updating persisted grant. Entity ID {1}",
                    entityId
                );
                return StoreResult.Failure(ErrorDescriber.ConcurrencyException);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error updating persisted grant. Entity ID {1}",
                    entityId
                );
                return StoreResult.Failure(ErrorDescriber.GeneralError);
            }
        }

        protected virtual IRavenQueryable<Entities.PersistedGrant> ApplyFilter(
            IRavenQueryable<Entities.PersistedGrant> query,
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