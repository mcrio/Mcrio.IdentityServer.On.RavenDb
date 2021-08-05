using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

// ReSharper disable SA1600
#pragma warning disable 1591
#pragma warning disable 8618

namespace Mcrio.IdentityServer.On.RavenDb.Storage.Entities
{
    /// <summary>
    /// IDS 4 Api Resource.
    /// </summary>
    [SuppressMessage("ReSharper", "SA1600", Justification = "Check IDS4 documentation for property descriptions.")]
    public class ApiResource : IEntity
    {
        public string Id { get; set; }

        public bool Enabled { get; set; } = true;

        public string Name { get; set; }

        public string DisplayName { get; set; }

        public string Description { get; set; }

        public List<string> AllowedAccessTokenSigningAlgorithms { get; set; } = new List<string>();

        public bool ShowInDiscoveryDocument { get; set; } = true;

        public List<ApiResourceSecret> Secrets { get; set; } = new List<ApiResourceSecret>();

        public List<string> Scopes { get; set; } = new List<string>();

        public List<string> UserClaims { get; set; } = new List<string>();

        public Dictionary<string, string> Properties { get; set; } = new Dictionary<string, string>();

        public DateTime Created { get; set; } = DateTime.UtcNow;

        public DateTime? Updated { get; set; }

        public DateTime? LastAccessed { get; set; }

        public bool NonEditable { get; set; }
    }
}