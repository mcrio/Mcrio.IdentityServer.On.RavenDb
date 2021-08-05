using System;
using System.Diagnostics.CodeAnalysis;

#pragma warning disable 1591
#pragma warning disable 8618

namespace Mcrio.IdentityServer.On.RavenDb.Storage.Entities
{
    /// <summary>
    /// IDS4 Persisted Grant.
    /// </summary>
    [SuppressMessage("ReSharper", "SA1600", Justification = "Check IDS4 documentation for property descriptions.")]
    public class PersistedGrant : IEntity
    {
        public string Id { get; set; }

        public string Key { get; set; }

        public string Type { get; set; }

        public string SubjectId { get; set; }

        public string SessionId { get; set; }

        public string ClientId { get; set; }

        public string Description { get; set; }

        public DateTime CreationTime { get; set; }

        public DateTime? Expiration { get; set; }

        public DateTime? ConsumedTime { get; set; }

        public string Data { get; set; }
    }
}