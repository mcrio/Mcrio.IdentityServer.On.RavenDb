using System;
using System.Threading;
using System.Threading.Tasks;
using IdentityServer4.Models;
using Mcrio.IdentityServer.On.RavenDb.Storage.Mappers;
using Microsoft.Extensions.Logging;
using Raven.Client.Documents.Session;
using Raven.Client.Exceptions;

namespace Mcrio.IdentityServer.On.RavenDb.Storage.Stores
{
    public class ResourceStoreExtension : ResourceStoreExtension<IdentityResource, Entities.IdentityResource,
        ApiResource, Entities.ApiResource, ApiScope, Entities.ApiScope>
    {
        public ResourceStoreExtension(
            IdentityServerDocumentSessionProvider identityServerDocumentSessionProvider,
            IIdentityServerStoreMapper mapper,
            ILogger<ResourceStoreExtension<IdentityResource, Entities.IdentityResource, ApiResource,
                Entities.ApiResource, ApiScope, Entities.ApiScope>> logger)
            : base(identityServerDocumentSessionProvider, mapper, logger)
        {
        }
    }

    public abstract class ResourceStoreExtension<TIdentityResourceModel, TIdentityResourceEntity,
            TApiResourceModel, TApiResourceEntity, TApiScopeModel, TApiScopeEntity>
        : IResourceStoreExtension<TIdentityResourceModel, TApiResourceModel, TApiScopeModel>
        where TIdentityResourceModel : IdentityResource
        where TIdentityResourceEntity : Entities.IdentityResource
        where TApiResourceModel : ApiResource
        where TApiResourceEntity : Entities.ApiResource
        where TApiScopeModel : ApiScope
        where TApiScopeEntity : Entities.ApiScope
    {
        protected ResourceStoreExtension(
            IdentityServerDocumentSessionProvider identityServerDocumentSessionProvider,
            IIdentityServerStoreMapper mapper,
            ILogger<ResourceStoreExtension<TIdentityResourceModel, TIdentityResourceEntity,
                TApiResourceModel, TApiResourceEntity, TApiScopeModel, TApiScopeEntity>> logger)
        {
            DocumentSession = identityServerDocumentSessionProvider();
            Mapper = mapper;
            Logger = logger;
        }

        protected IAsyncDocumentSession DocumentSession { get; }

        protected IIdentityServerStoreMapper Mapper { get; }

        protected ILogger<ResourceStoreExtension<TIdentityResourceModel, TIdentityResourceEntity,
            TApiResourceModel, TApiResourceEntity, TApiScopeModel, TApiScopeEntity>> Logger { get; }

        public virtual async Task<StoreResult> CreateIdentityResourceAsync(
            TIdentityResourceModel identityResource,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (identityResource == null)
            {
                throw new ArgumentNullException(nameof(identityResource));
            }

            TIdentityResourceEntity entity =
                Mapper.ToEntity<TIdentityResourceModel, TIdentityResourceEntity>(identityResource);

            if (!CheckRequiredFields(entity, out string errorMsg))
            {
                return StoreResult.Failure(errorMsg);
            }

            try
            {
                await DocumentSession
                    .StoreAsync(
                        entity,
                        string.Empty,
                        entity.Id,
                        cancellationToken
                    )
                    .ConfigureAwait(false);
                await DocumentSession.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                return StoreResult.Success();
            }
            catch (ConcurrencyException concurrencyException)
            {
                Logger.LogError(
                    concurrencyException,
                    "Error creating identity resource. Resource name {0}; Entity ID {1}",
                    identityResource.Name,
                    entity.Id
                );
                return StoreResult.Failure(ErrorDescriber.ConcurrencyException);
            }
            catch (Exception ex)
            {
                Logger.LogError(
                    ex,
                    "Error creating identity resource. Resource name {0}; Entity ID {1}",
                    identityResource.Name,
                    entity.Id
                );
                return StoreResult.Failure(ErrorDescriber.GeneralError);
            }
        }

        public virtual async Task<StoreResult> UpdateIdentityResourceAsync(
            TIdentityResourceModel identityResource,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (identityResource == null)
            {
                throw new ArgumentNullException(nameof(identityResource));
            }

            TIdentityResourceEntity updatedEntity =
                Mapper.ToEntity<TIdentityResourceModel, TIdentityResourceEntity>(identityResource);

            if (!CheckRequiredFields(updatedEntity, out string errorMsg))
            {
                return StoreResult.Failure(errorMsg);
            }

            string entityId = updatedEntity.Id;
            TIdentityResourceEntity entityInSession = await DocumentSession
                .LoadAsync<TIdentityResourceEntity>(entityId, cancellationToken)
                .ConfigureAwait(false);

            if (entityInSession is null)
            {
                return StoreResult.Failure(string.Format(ErrorDescriber.EntityNotFound, entityId));
            }

            Mapper.Map(updatedEntity, entityInSession);

            try
            {
                string changeVector = DocumentSession.Advanced.GetChangeVectorFor(entityInSession);
                await DocumentSession
                    .StoreAsync(entityInSession, changeVector, entityInSession.Id, cancellationToken)
                    .ConfigureAwait(false);
                await DocumentSession.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                return StoreResult.Success();
            }
            catch (ConcurrencyException concurrencyException)
            {
                Logger.LogError(
                    concurrencyException,
                    "Error updating identity resource. Entity ID {1}",
                    entityId
                );
                return StoreResult.Failure(ErrorDescriber.ConcurrencyException);
            }
            catch (Exception ex)
            {
                Logger.LogError(
                    ex,
                    "Error updating identity resource. Entity ID {1}",
                    entityId
                );
                return StoreResult.Failure(ErrorDescriber.GeneralError);
            }
        }

        public virtual async Task<StoreResult> DeleteIdentityResourceAsync(
            string identityResourceName,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(identityResourceName))
            {
                throw new ArgumentNullException(nameof(identityResourceName));
            }

            string entityId = Mapper.CreateEntityId<TIdentityResourceEntity>(identityResourceName);

            TIdentityResourceEntity entityInSession = await DocumentSession
                .LoadAsync<TIdentityResourceEntity>(
                    entityId,
                    cancellationToken)
                .ConfigureAwait(false);

            if (entityInSession is null)
            {
                return StoreResult.Failure(string.Format(ErrorDescriber.EntityNotFound, entityId));
            }

            try
            {
                string changeVector = DocumentSession.Advanced.GetChangeVectorFor(entityInSession);
                DocumentSession.Delete(entityId, changeVector);
                await DocumentSession.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                return StoreResult.Success();
            }
            catch (ConcurrencyException concurrencyException)
            {
                Logger.LogError(
                    concurrencyException,
                    "Error deleting identity resource. Entity ID {1}",
                    entityId
                );
                return StoreResult.Failure(ErrorDescriber.ConcurrencyException);
            }
            catch (Exception ex)
            {
                Logger.LogError(
                    ex,
                    "Error deleting identity resource. Entity ID {1}",
                    entityId
                );
                return StoreResult.Failure(ErrorDescriber.GeneralError);
            }
        }

        public virtual async Task<StoreResult> CreateApiResourceAsync(
            TApiResourceModel apiResource,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (apiResource == null)
            {
                throw new ArgumentNullException(nameof(apiResource));
            }

            TApiResourceEntity entity = Mapper.ToEntity<TApiResourceModel, TApiResourceEntity>(apiResource);

            if (!CheckRequiredFields(entity, out string errorMsg))
            {
                return StoreResult.Failure(errorMsg);
            }

            try
            {
                await DocumentSession
                    .StoreAsync(
                        entity,
                        string.Empty,
                        entity.Id,
                        cancellationToken
                    )
                    .ConfigureAwait(false);
                await DocumentSession.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                return StoreResult.Success();
            }
            catch (ConcurrencyException concurrencyException)
            {
                Logger.LogError(
                    concurrencyException,
                    "Error creating api resource. Resource name {0}; Entity ID {1}",
                    apiResource.Name,
                    entity.Id
                );
                return StoreResult.Failure(ErrorDescriber.ConcurrencyException);
            }
            catch (Exception ex)
            {
                Logger.LogError(
                    ex,
                    "Error creating api resource. Resource name {0}; Entity ID {1}",
                    apiResource.Name,
                    entity.Id
                );
                return StoreResult.Failure(ErrorDescriber.GeneralError);
            }
        }

        /// <inheritdoc/>
        public virtual async Task<StoreResult> UpdateApiResourceAsync(
            TApiResourceModel apiResource,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (apiResource == null)
            {
                throw new ArgumentNullException(nameof(apiResource));
            }

            TApiResourceEntity updatedEntity = Mapper.ToEntity<TApiResourceModel, TApiResourceEntity>(apiResource);

            if (!CheckRequiredFields(updatedEntity, out string errorMsg))
            {
                return StoreResult.Failure(errorMsg);
            }

            string entityId = updatedEntity.Id;
            TApiResourceEntity entityInSession = await DocumentSession
                .LoadAsync<TApiResourceEntity>(entityId, cancellationToken)
                .ConfigureAwait(false);

            if (entityInSession is null)
            {
                return StoreResult.Failure(string.Format(ErrorDescriber.EntityNotFound, entityId));
            }

            Mapper.Map(updatedEntity, entityInSession);

            try
            {
                string changeVector = DocumentSession.Advanced.GetChangeVectorFor(entityInSession);
                await DocumentSession
                    .StoreAsync(entityInSession, changeVector, entityInSession.Id, cancellationToken)
                    .ConfigureAwait(false);
                await DocumentSession.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                return StoreResult.Success();
            }
            catch (ConcurrencyException concurrencyException)
            {
                Logger.LogError(
                    concurrencyException,
                    "Error updating api resource. Entity ID {1}",
                    entityId
                );
                return StoreResult.Failure(ErrorDescriber.ConcurrencyException);
            }
            catch (Exception ex)
            {
                Logger.LogError(
                    ex,
                    "Error updating api resource. Entity ID {1}",
                    entityId
                );
                return StoreResult.Failure(ErrorDescriber.GeneralError);
            }
        }

        public virtual async Task<StoreResult> DeleteApiResourceAsync(
            string apiResourceName,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(apiResourceName))
            {
                throw new ArgumentNullException(nameof(apiResourceName));
            }

            string entityId = Mapper.CreateEntityId<TApiResourceEntity>(apiResourceName);

            TApiResourceEntity entityInSession = await DocumentSession
                .LoadAsync<TApiResourceEntity>(
                    entityId,
                    cancellationToken)
                .ConfigureAwait(false);

            if (entityInSession is null)
            {
                return StoreResult.Failure(string.Format(ErrorDescriber.EntityNotFound, entityId));
            }

            try
            {
                string changeVector = DocumentSession.Advanced.GetChangeVectorFor(entityInSession);
                DocumentSession.Delete(entityId, changeVector);
                await DocumentSession.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                return StoreResult.Success();
            }
            catch (ConcurrencyException concurrencyException)
            {
                Logger.LogError(
                    concurrencyException,
                    "Error deleting api resource. Entity ID {1}",
                    entityId
                );
                return StoreResult.Failure(ErrorDescriber.ConcurrencyException);
            }
            catch (Exception ex)
            {
                Logger.LogError(
                    ex,
                    "Error deleting api resource. Entity ID {1}",
                    entityId
                );
                return StoreResult.Failure(ErrorDescriber.GeneralError);
            }
        }

        public virtual async Task<StoreResult> CreateApiScopeAsync(
            TApiScopeModel apiScope,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (apiScope == null)
            {
                throw new ArgumentNullException(nameof(apiScope));
            }

            TApiScopeEntity entity = Mapper.ToEntity<TApiScopeModel, TApiScopeEntity>(apiScope);

            if (!CheckRequiredFields(entity, out string errorMsg))
            {
                return StoreResult.Failure(errorMsg);
            }

            try
            {
                await DocumentSession
                    .StoreAsync(
                        entity,
                        string.Empty,
                        entity.Id,
                        cancellationToken
                    )
                    .ConfigureAwait(false);
                await DocumentSession.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                return StoreResult.Success();
            }
            catch (ConcurrencyException concurrencyException)
            {
                Logger.LogError(
                    concurrencyException,
                    "Error creating api scope. Scope name {0}; Entity ID {1}",
                    apiScope.Name,
                    entity.Id
                );
                return StoreResult.Failure(ErrorDescriber.ConcurrencyException);
            }
            catch (Exception ex)
            {
                Logger.LogError(
                    ex,
                    "Error creating api scope. Scope name {0}; Entity ID {1}",
                    apiScope.Name,
                    entity.Id
                );
                return StoreResult.Failure(ErrorDescriber.GeneralError);
            }
        }

        public virtual async Task<StoreResult> UpdateApiScopeAsync(
            TApiScopeModel apiScope,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (apiScope == null)
            {
                throw new ArgumentNullException(nameof(apiScope));
            }

            TApiScopeEntity updatedEntity = Mapper.ToEntity<TApiScopeModel, TApiScopeEntity>(apiScope);

            if (!CheckRequiredFields(updatedEntity, out string errorMsg))
            {
                return StoreResult.Failure(errorMsg);
            }

            string entityId = updatedEntity.Id;
            TApiScopeEntity entityInSession = await DocumentSession
                .LoadAsync<TApiScopeEntity>(entityId, cancellationToken)
                .ConfigureAwait(false);

            if (entityInSession is null)
            {
                return StoreResult.Failure(string.Format(ErrorDescriber.EntityNotFound, entityId));
            }

            Mapper.Map(updatedEntity, entityInSession);

            try
            {
                string changeVector = DocumentSession.Advanced.GetChangeVectorFor(entityInSession);
                await DocumentSession
                    .StoreAsync(entityInSession, changeVector, entityInSession.Id, cancellationToken)
                    .ConfigureAwait(false);
                await DocumentSession.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                return StoreResult.Success();
            }
            catch (ConcurrencyException concurrencyException)
            {
                Logger.LogError(
                    concurrencyException,
                    "Error updating api scope. Entity ID {1}",
                    entityId
                );
                return StoreResult.Failure(ErrorDescriber.ConcurrencyException);
            }
            catch (Exception ex)
            {
                Logger.LogError(
                    ex,
                    "Error updating api scope. Entity ID {1}",
                    entityId
                );
                return StoreResult.Failure(ErrorDescriber.GeneralError);
            }
        }

        public virtual async Task<StoreResult> DeleteApiScopeAsync(
            string apiScopeName,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(apiScopeName))
            {
                throw new ArgumentNullException(nameof(apiScopeName));
            }

            string entityId = Mapper.CreateEntityId<TApiScopeEntity>(apiScopeName);

            TApiScopeEntity entityInSession = await DocumentSession
                .LoadAsync<TApiScopeEntity>(
                    entityId,
                    cancellationToken)
                .ConfigureAwait(false);

            if (entityInSession is null)
            {
                return StoreResult.Failure(string.Format(ErrorDescriber.EntityNotFound, entityId));
            }

            try
            {
                string changeVector = DocumentSession.Advanced.GetChangeVectorFor(entityInSession);
                DocumentSession.Delete(entityId, changeVector);
                await DocumentSession.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                return StoreResult.Success();
            }
            catch (ConcurrencyException concurrencyException)
            {
                Logger.LogError(
                    concurrencyException,
                    "Error deleting api scope. Entity ID {1}",
                    entityId
                );
                return StoreResult.Failure(ErrorDescriber.ConcurrencyException);
            }
            catch (Exception ex)
            {
                Logger.LogError(
                    ex,
                    "Error deleting api scope. Entity ID {1}",
                    entityId
                );
                return StoreResult.Failure(ErrorDescriber.GeneralError);
            }
        }

        protected virtual bool CheckRequiredFields(TIdentityResourceEntity identityResource, out string errorMessage)
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

        protected virtual bool CheckRequiredFields(TApiResourceEntity apiResource, out string errorMessage)
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

        protected virtual bool CheckRequiredFields(TApiScopeEntity apiScope, out string errorMessage)
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