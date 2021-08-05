using System.Threading;
using System.Threading.Tasks;
using IdentityServer4.Models;

namespace Mcrio.IdentityServer.On.RavenDb.Storage.Stores
{
    /// <summary>
    /// Additional methods that allows create, update and delete of resources.
    /// </summary>
    /// <typeparam name="TIdentityResourceModel">Type of identity resource model.</typeparam>
    /// <typeparam name="TApiResourceModel">Type of API resource model.</typeparam>
    /// <typeparam name="TApiScopeModel">Type of api scope model.</typeparam>
    public interface IResourceStoreExtension<TIdentityResourceModel, TApiResourceModel, TApiScopeModel>
        where TIdentityResourceModel : IdentityResource
        where TApiResourceModel : ApiResource
        where TApiScopeModel : ApiScope
    {
        /// <summary>
        /// Creates and identity resource.
        /// </summary>
        /// <param name="identityResource">Identity resource.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The <see cref="Task"/> that represents the asynchronous operation.</returns>
        public Task<StoreResult> CreateIdentityResourceAsync(
            TIdentityResourceModel identityResource,
            CancellationToken cancellationToken = default
        );

        /// <summary>
        /// Update identity resource.
        /// </summary>
        /// <param name="identityResource">Identity resource.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The <see cref="Task"/> that represents the asynchronous operation.</returns>
        public Task<StoreResult> UpdateIdentityResourceAsync(
            TIdentityResourceModel identityResource,
            CancellationToken cancellationToken = default
        );

        /// <summary>
        /// Delete identity resource.
        /// </summary>
        /// <param name="identityResourceName">Identity resource name.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The <see cref="Task"/> that represents the asynchronous operation.</returns>
        public Task<StoreResult> DeleteIdentityResourceAsync(
            string identityResourceName,
            CancellationToken cancellationToken = default
        );

        /// <summary>
        /// Create API resource.
        /// </summary>
        /// <param name="apiResource">API resource.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The <see cref="Task"/> that represents the asynchronous operation.</returns>
        public Task<StoreResult> CreateApiResourceAsync(
            TApiResourceModel apiResource,
            CancellationToken cancellationToken = default
        );

        /// <summary>
        /// Update API resource.
        /// </summary>
        /// <param name="apiResource">API resource.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The <see cref="Task"/> that represents the asynchronous operation.</returns>
        public Task<StoreResult> UpdateApiResourceAsync(
            TApiResourceModel apiResource,
            CancellationToken cancellationToken = default
        );

        /// <summary>
        /// Delete API resource.
        /// </summary>
        /// <param name="apiResourceName">API resource name.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The <see cref="Task"/> that represents the asynchronous operation.</returns>
        public Task<StoreResult> DeleteApiResourceAsync(
            string apiResourceName,
            CancellationToken cancellationToken = default
        );

        /// <summary>
        /// Create API scope.
        /// </summary>
        /// <param name="apiScope">API scope.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The <see cref="Task"/> that represents the asynchronous operation.</returns>
        public Task<StoreResult> CreateApiScopeAsync(
            TApiScopeModel apiScope,
            CancellationToken cancellationToken = default
        );

        /// <summary>
        /// Update API scope.
        /// </summary>
        /// <param name="apiScope">API scope.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The <see cref="Task"/> that represents the asynchronous operation.</returns>
        public Task<StoreResult> UpdateApiScopeAsync(
            TApiScopeModel apiScope,
            CancellationToken cancellationToken = default
        );

        /// <summary>
        /// Delete API scope.
        /// </summary>
        /// <param name="apiScopeName">API scope name.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The <see cref="Task"/> that represents the asynchronous operation.</returns>
        public Task<StoreResult> DeleteApiScopeAsync(
            string apiScopeName,
            CancellationToken cancellationToken = default
        );
    }
}