using System;
using System.Diagnostics.CodeAnalysis;

#pragma warning disable 8618
#pragma warning disable 1591

namespace Mcrio.IdentityServer.On.RavenDb.Storage.Entities
{
    /// <summary>
    /// IDS4 Secret.
    /// </summary>
    [SuppressMessage("ReSharper", "SA1600", Justification = "Check IDS4 documentation for property descriptions.")]
    public abstract class Secret
    {
        public string Description { get; set; }

        public string Value { get; set; }

        public DateTime? Expiration { get; set; }

        public string Type { get; set; } = "SharedSecret";

        public DateTime Created { get; set; } = DateTime.UtcNow;
    }
}