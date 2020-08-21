using System;
using System.Threading;
using System.Threading.Tasks;
using IdentityServer4.Models;
using Mcrio.IdentityServer.On.RavenDb.Storage.Mappers;
using Mcrio.IdentityServer.On.RavenDb.Storage.Stores.Advanced;
using Microsoft.Extensions.Logging;
using Raven.Client.Documents.Session;
using Raven.Client.Exceptions;

namespace Mcrio.IdentityServer.On.RavenDb.Storage.Stores
{
    internal class ResourceStoreAdditions : IResourceStoreAdditions
    {
        private readonly IAsyncDocumentSession _documentSession;
        private readonly IIdentityServerStoreMapper _mapper;
        private readonly ILogger<ResourceStore> _logger;

        public ResourceStoreAdditions(
            IdentityServerDocumentSessionProvider identityServerDocumentSessionProvider,
            IIdentityServerStoreMapper mapper,
            ILogger<ResourceStore> logger)
        {
            _documentSession = identityServerDocumentSessionProvider();
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<StoreResult> CreateIdentityResourceAsync(
            IdentityResource identityResource,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (identityResource == null)
            {
                throw new ArgumentNullException(nameof(identityResource));
            }

            Entities.IdentityResource entity = _mapper.ToEntity(identityResource);

            if (!CheckRequiredFields(entity, out string errorMsg))
            {
                return StoreResult.Failure(errorMsg);
            }

            try
            {
                await _documentSession
                    .StoreAsync(
                        entity,
                        string.Empty,
                        entity.Id,
                        cancellationToken
                    )
                    .ConfigureAwait(false);
                await _documentSession.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                return StoreResult.Success();
            }
            catch (ConcurrencyException concurrencyException)
            {
                _logger.LogError(
                    concurrencyException,
                    "Error creating identity resource. Resource name {0}; Entity ID {1}",
                    identityResource.Name,
                    entity.Id
                );
                return StoreResult.Failure(ErrorDescriber.ConcurrencyException);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error creating identity resource. Resource name {0}; Entity ID {1}",
                    identityResource.Name,
                    entity.Id
                );
                return StoreResult.Failure(ErrorDescriber.GeneralError);
            }
        }

        public async Task<StoreResult> UpdateIdentityResourceAsync(IdentityResource identityResource,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (identityResource == null)
            {
                throw new ArgumentNullException(nameof(identityResource));
            }

            Entities.IdentityResource updatedEntity = _mapper.ToEntity(identityResource);

            if (!CheckRequiredFields(updatedEntity, out string errorMsg))
            {
                return StoreResult.Failure(errorMsg);
            }

            string entityId = updatedEntity.Id;
            Entities.IdentityResource entityInSession = await _documentSession
                .LoadAsync<Entities.IdentityResource>(entityId, cancellationToken)
                .ConfigureAwait(false);

            if (entityInSession is null)
            {
                return StoreResult.Failure(string.Format(ErrorDescriber.EntityNotFound, entityId));
            }

            _mapper.Map(updatedEntity, entityInSession);

            try
            {
                string changeVector = _documentSession.Advanced.GetChangeVectorFor(entityInSession);
                await _documentSession
                    .StoreAsync(entityInSession, changeVector, entityInSession.Id, cancellationToken)
                    .ConfigureAwait(false);
                await _documentSession.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                return StoreResult.Success();
            }
            catch (ConcurrencyException concurrencyException)
            {
                _logger.LogError(
                    concurrencyException,
                    "Error updating identity resource. Entity ID {1}",
                    entityId
                );
                return StoreResult.Failure(ErrorDescriber.ConcurrencyException);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error updating identity resource. Entity ID {1}",
                    entityId
                );
                return StoreResult.Failure(ErrorDescriber.GeneralError);
            }
        }

        public async Task<StoreResult> DeleteIdentityResourceAsync(
            string identityResourceName,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(identityResourceName))
            {
                throw new ArgumentNullException(nameof(identityResourceName));
            }

            string entityId = _mapper.CreateEntityId<Entities.IdentityResource>(identityResourceName);

            Entities.IdentityResource entityInSession = await _documentSession
                .LoadAsync<Entities.IdentityResource>(
                    entityId,
                    cancellationToken)
                .ConfigureAwait(false);

            if (entityInSession is null)
            {
                return StoreResult.Failure(string.Format(ErrorDescriber.EntityNotFound, entityId));
            }

            try
            {
                string changeVector = _documentSession.Advanced.GetChangeVectorFor(entityInSession);
                _documentSession.Delete(entityId, changeVector);
                await _documentSession.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                return StoreResult.Success();
            }
            catch (ConcurrencyException concurrencyException)
            {
                _logger.LogError(
                    concurrencyException,
                    "Error deleting identity resource. Entity ID {1}",
                    entityId
                );
                return StoreResult.Failure(ErrorDescriber.ConcurrencyException);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error deleting identity resource. Entity ID {1}",
                    entityId
                );
                return StoreResult.Failure(ErrorDescriber.GeneralError);
            }
        }

        public async Task<StoreResult> CreateApiResourceAsync(
            ApiResource apiResource,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (apiResource == null)
            {
                throw new ArgumentNullException(nameof(apiResource));
            }

            Entities.ApiResource entity = _mapper.ToEntity(apiResource);

            if (!CheckRequiredFields(entity, out string errorMsg))
            {
                return StoreResult.Failure(errorMsg);
            }

            try
            {
                await _documentSession
                    .StoreAsync(
                        entity,
                        string.Empty,
                        entity.Id,
                        cancellationToken
                    )
                    .ConfigureAwait(false);
                await _documentSession.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                return StoreResult.Success();
            }
            catch (ConcurrencyException concurrencyException)
            {
                _logger.LogError(
                    concurrencyException,
                    "Error creating api resource. Resource name {0}; Entity ID {1}",
                    apiResource.Name,
                    entity.Id
                );
                return StoreResult.Failure(ErrorDescriber.ConcurrencyException);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error creating api resource. Resource name {0}; Entity ID {1}",
                    apiResource.Name,
                    entity.Id
                );
                return StoreResult.Failure(ErrorDescriber.GeneralError);
            }
        }

        /// <inheritdoc/>
        public async Task<StoreResult> UpdateApiResourceAsync(
            ApiResource apiResource,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (apiResource == null)
            {
                throw new ArgumentNullException(nameof(apiResource));
            }

            Entities.ApiResource updatedEntity = _mapper.ToEntity(apiResource);

            if (!CheckRequiredFields(updatedEntity, out string errorMsg))
            {
                return StoreResult.Failure(errorMsg);
            }

            string entityId = updatedEntity.Id;
            Entities.ApiResource entityInSession = await _documentSession
                .LoadAsync<Entities.ApiResource>(entityId, cancellationToken)
                .ConfigureAwait(false);

            if (entityInSession is null)
            {
                return StoreResult.Failure(string.Format(ErrorDescriber.EntityNotFound, entityId));
            }

            _mapper.Map(updatedEntity, entityInSession);

            try
            {
                string changeVector = _documentSession.Advanced.GetChangeVectorFor(entityInSession);
                await _documentSession
                    .StoreAsync(entityInSession, changeVector, entityInSession.Id, cancellationToken)
                    .ConfigureAwait(false);
                await _documentSession.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                return StoreResult.Success();
            }
            catch (ConcurrencyException concurrencyException)
            {
                _logger.LogError(
                    concurrencyException,
                    "Error updating api resource. Entity ID {1}",
                    entityId
                );
                return StoreResult.Failure(ErrorDescriber.ConcurrencyException);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error updating api resource. Entity ID {1}",
                    entityId
                );
                return StoreResult.Failure(ErrorDescriber.GeneralError);
            }
        }

        public async Task<StoreResult> DeleteApiResourceAsync(
            string apiResourceName,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(apiResourceName))
            {
                throw new ArgumentNullException(nameof(apiResourceName));
            }

            string entityId = _mapper.CreateEntityId<Entities.ApiResource>(apiResourceName);

            Entities.ApiResource entityInSession = await _documentSession
                .LoadAsync<Entities.ApiResource>(
                    entityId,
                    cancellationToken)
                .ConfigureAwait(false);

            if (entityInSession is null)
            {
                return StoreResult.Failure(string.Format(ErrorDescriber.EntityNotFound, entityId));
            }

            try
            {
                string changeVector = _documentSession.Advanced.GetChangeVectorFor(entityInSession);
                _documentSession.Delete(entityId, changeVector);
                await _documentSession.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                return StoreResult.Success();
            }
            catch (ConcurrencyException concurrencyException)
            {
                _logger.LogError(
                    concurrencyException,
                    "Error deleting api resource. Entity ID {1}",
                    entityId
                );
                return StoreResult.Failure(ErrorDescriber.ConcurrencyException);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error deleting api resource. Entity ID {1}",
                    entityId
                );
                return StoreResult.Failure(ErrorDescriber.GeneralError);
            }
        }

        public async Task<StoreResult> CreateApiScopeAsync(ApiScope apiScope,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (apiScope == null)
            {
                throw new ArgumentNullException(nameof(apiScope));
            }

            Entities.ApiScope entity = _mapper.ToEntity(apiScope);

            if (!CheckRequiredFields(entity, out string errorMsg))
            {
                return StoreResult.Failure(errorMsg);
            }

            try
            {
                await _documentSession
                    .StoreAsync(
                        entity,
                        string.Empty,
                        entity.Id,
                        cancellationToken
                    )
                    .ConfigureAwait(false);
                await _documentSession.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                return StoreResult.Success();
            }
            catch (ConcurrencyException concurrencyException)
            {
                _logger.LogError(
                    concurrencyException,
                    "Error creating api scope. Scope name {0}; Entity ID {1}",
                    apiScope.Name,
                    entity.Id
                );
                return StoreResult.Failure(ErrorDescriber.ConcurrencyException);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error creating api scope. Scope name {0}; Entity ID {1}",
                    apiScope.Name,
                    entity.Id
                );
                return StoreResult.Failure(ErrorDescriber.GeneralError);
            }
        }

        public async Task<StoreResult> UpdateApiScopeAsync(ApiScope apiScope,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (apiScope == null)
            {
                throw new ArgumentNullException(nameof(apiScope));
            }

            Entities.ApiScope updatedEntity = _mapper.ToEntity(apiScope);

            if (!CheckRequiredFields(updatedEntity, out string errorMsg))
            {
                return StoreResult.Failure(errorMsg);
            }

            string entityId = updatedEntity.Id;
            Entities.ApiScope entityInSession = await _documentSession
                .LoadAsync<Entities.ApiScope>(entityId, cancellationToken)
                .ConfigureAwait(false);

            if (entityInSession is null)
            {
                return StoreResult.Failure(string.Format(ErrorDescriber.EntityNotFound, entityId));
            }

            _mapper.Map(updatedEntity, entityInSession);

            try
            {
                string changeVector = _documentSession.Advanced.GetChangeVectorFor(entityInSession);
                await _documentSession
                    .StoreAsync(entityInSession, changeVector, entityInSession.Id, cancellationToken)
                    .ConfigureAwait(false);
                await _documentSession.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                return StoreResult.Success();
            }
            catch (ConcurrencyException concurrencyException)
            {
                _logger.LogError(
                    concurrencyException,
                    "Error updating api scope. Entity ID {1}",
                    entityId
                );
                return StoreResult.Failure(ErrorDescriber.ConcurrencyException);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error updating api scope. Entity ID {1}",
                    entityId
                );
                return StoreResult.Failure(ErrorDescriber.GeneralError);
            }
        }

        public async Task<StoreResult> DeleteApiScopeAsync(string apiScopeName,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(apiScopeName))
            {
                throw new ArgumentNullException(nameof(apiScopeName));
            }

            string entityId = _mapper.CreateEntityId<Entities.ApiScope>(apiScopeName);

            Entities.ApiScope entityInSession = await _documentSession
                .LoadAsync<Entities.ApiScope>(
                    entityId,
                    cancellationToken)
                .ConfigureAwait(false);

            if (entityInSession is null)
            {
                return StoreResult.Failure(string.Format(ErrorDescriber.EntityNotFound, entityId));
            }

            try
            {
                string changeVector = _documentSession.Advanced.GetChangeVectorFor(entityInSession);
                _documentSession.Delete(entityId, changeVector);
                await _documentSession.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                return StoreResult.Success();
            }
            catch (ConcurrencyException concurrencyException)
            {
                _logger.LogError(
                    concurrencyException,
                    "Error deleting api scope. Entity ID {1}",
                    entityId
                );
                return StoreResult.Failure(ErrorDescriber.ConcurrencyException);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error deleting api scope. Entity ID {1}",
                    entityId
                );
                return StoreResult.Failure(ErrorDescriber.GeneralError);
            }
        }

        private static bool CheckRequiredFields(Entities.IdentityResource identityResource, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(identityResource.Name))
            {
                errorMessage = ErrorDescriber.IdentityResourceNameMissing;
                return false;
            }

            if (string.IsNullOrWhiteSpace(identityResource.Id))
            {
                errorMessage = ErrorDescriber.IdMustNotBeEmpty;
                return false;
            }

            return true;
        }

        private static bool CheckRequiredFields(Entities.ApiResource apiResource, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(apiResource.Name))
            {
                errorMessage = ErrorDescriber.ApiResourceNameMissing;
                return false;
            }

            if (string.IsNullOrWhiteSpace(apiResource.Id))
            {
                errorMessage = ErrorDescriber.IdMustNotBeEmpty;
                return false;
            }

            return true;
        }

        private static bool CheckRequiredFields(Entities.ApiScope apiScope, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(apiScope.Name))
            {
                errorMessage = ErrorDescriber.ApiScopeNameMissing;
                return false;
            }

            if (string.IsNullOrWhiteSpace(apiScope.Id))
            {
                errorMessage = ErrorDescriber.IdMustNotBeEmpty;
                return false;
            }

            return true;
        }
    }
}