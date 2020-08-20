using System.Threading;
using System.Threading.Tasks;
using IdentityServer4.Models;
using IdentityServer4.Stores;

namespace Mcrio.IdentityServer.On.RavenDb.Storage.Stores.Advanced
{
    public interface IClientStoreAdditions
    {
        Task<StoreResult> CreateAsync(Client client, CancellationToken cancellationToken = default);

        Task<StoreResult> UpdateAsync(Client client, CancellationToken cancellationToken = default);
        
        Task<StoreResult> DeleteAsync(string clientId, CancellationToken cancellationToken = default);
    }
}