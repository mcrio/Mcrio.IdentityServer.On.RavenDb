using System.Threading;
using System.Threading.Tasks;
using IdentityServer4.Models;

namespace Mcrio.IdentityServer.On.RavenDb.Storage.Stores
{
    public interface IClientStoreExtension<TClientModel>
        where TClientModel : Client
    {
        Task<StoreResult> CreateAsync(TClientModel client, CancellationToken cancellationToken = default);

        Task<StoreResult> UpdateAsync(TClientModel client, CancellationToken cancellationToken = default);

        Task<StoreResult> DeleteAsync(string clientId, CancellationToken cancellationToken = default);
    }
}