#pragma warning disable 8618
namespace Mcrio.IdentityServer.On.RavenDb.Storage.Entities
{
    /// <summary>
    /// IDS4 Client claim.
    /// </summary>
    public class ClientClaim
    {
        /// <summary>
        /// Claim type.
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// Claim value.
        /// </summary>
        public string Value { get; set; }
    }
}