using System.Threading;
using System.Threading.Tasks;
using IdentityServer4.Models;

namespace Mcrio.IdentityServer.On.RavenDb.Storage.Stores
{
    public interface IResourceStoreExtension<TIdentityResourceModel, TApiResourceModel, TApiScopeModel>
        where TIdentityResourceModel : IdentityResource
        where TApiResourceModel : ApiResource
        where TApiScopeModel : ApiScope
    {
        public Task<StoreResult> CreateIdentityResourceAsync(
            TIdentityResourceModel identityResource,
            CancellationToken cancellationToken = default
        );

        public Task<StoreResult> UpdateIdentityResourceAsync(
            TIdentityResourceModel identityResource,
            CancellationToken cancellationToken = default
        );

        public Task<StoreResult> DeleteIdentityResourceAsync(
            string identityResourceName,
            CancellationToken cancellationToken = default
        );

        public Task<StoreResult> CreateApiResourceAsync(
            TApiResourceModel apiResource,
            CancellationToken cancellationToken = default
        );

        public Task<StoreResult> UpdateApiResourceAsync(
            TApiResourceModel apiResource,
            CancellationToken cancellationToken = default
        );

        public Task<StoreResult> DeleteApiResourceAsync(
            string apiResourceName,
            CancellationToken cancellationToken = default
        );

        public Task<StoreResult> CreateApiScopeAsync(
            TApiScopeModel apiScope,
            CancellationToken cancellationToken = default
        );

        public Task<StoreResult> UpdateApiScopeAsync(
            TApiScopeModel apiScope,
            CancellationToken cancellationToken = default
        );

        public Task<StoreResult> DeleteApiScopeAsync(
            string apiScopeName,
            CancellationToken cancellationToken = default
        );
    }
}