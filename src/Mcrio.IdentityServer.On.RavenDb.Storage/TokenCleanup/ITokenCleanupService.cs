using System.Threading.Tasks;

namespace Mcrio.IdentityServer.On.RavenDb.Storage.TokenCleanup
{
    /// <summary>
    /// Helper to periodically cleanup expired persisted grants.
    /// </summary>
    public interface ITokenCleanupService
    {
        /// <summary>
        /// Method to clear expired persisted grants.
        /// </summary>
        /// <returns>The <see cref="Task"/> that represents the asynchronous operation.</returns>
        public Task RemoveExpiredGrantsAsync();
    }
}