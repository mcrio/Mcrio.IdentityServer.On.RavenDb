using System.Threading;
using System.Threading.Tasks;
using IdentityServer4.Models;

namespace Mcrio.IdentityServer.On.RavenDb.Storage.Stores.Additions
{
    public interface IClientStoreAdditions
    {
        Task<StoreResult> CreateAsync(Client client, CancellationToken cancellationToken = default);

        Task<StoreResult> UpdateAsync(Client client, CancellationToken cancellationToken = default);

        Task<StoreResult> DeleteAsync(string clientId, CancellationToken cancellationToken = default);
    }
}