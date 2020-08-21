using System.Threading;
using System.Threading.Tasks;
using IdentityServer4.Models;

namespace Mcrio.IdentityServer.On.RavenDb.Storage.Stores.Additions
{
    public interface IResourceStoreAdditions
    {
        public Task<StoreResult> CreateIdentityResourceAsync(
            IdentityResource identityResource,
            CancellationToken cancellationToken = default
        );

        public Task<StoreResult> UpdateIdentityResourceAsync(
            IdentityResource identityResource,
            CancellationToken cancellationToken = default
        );

        public Task<StoreResult> DeleteIdentityResourceAsync(
            string identityResourceName,
            CancellationToken cancellationToken = default
        );

        public Task<StoreResult> CreateApiResourceAsync(
            ApiResource apiResource,
            CancellationToken cancellationToken = default
        );

        public Task<StoreResult> UpdateApiResourceAsync(
            ApiResource apiResource,
            CancellationToken cancellationToken = default
        );

        public Task<StoreResult> DeleteApiResourceAsync(
            string apiResourceName,
            CancellationToken cancellationToken = default
        );

        public Task<StoreResult> CreateApiScopeAsync(
            ApiScope apiScope,
            CancellationToken cancellationToken = default
        );

        public Task<StoreResult> UpdateApiScopeAsync(
            ApiScope apiScope,
            CancellationToken cancellationToken = default
        );

        public Task<StoreResult> DeleteApiScopeAsync(
            string apiScopeName,
            CancellationToken cancellationToken = default
        );
    }
}