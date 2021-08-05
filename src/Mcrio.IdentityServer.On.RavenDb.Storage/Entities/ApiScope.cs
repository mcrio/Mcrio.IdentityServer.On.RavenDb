using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

#pragma warning disable 8618
#pragma warning disable 1591

namespace Mcrio.IdentityServer.On.RavenDb.Storage.Entities
{
    /// <summary>
    /// IDS4 API Scope.
    /// </summary>
    [SuppressMessage("ReSharper", "SA1600", Justification = "Check IDS4 documentation for property descriptions.")]
    public class ApiScope : IEntity
    {
        public string Id { get; set; }

        public bool Enabled { get; set; } = true;

        public string Name { get; set; }

        public string DisplayName { get; set; }

        public string Description { get; set; }

        public bool Required { get; set; }

        public bool Emphasize { get; set; }

        public bool ShowInDiscoveryDocument { get; set; } = true;

        public List<string> UserClaims { get; set; } = new List<string>();

        public Dictionary<string, string> Properties { get; set; } = new Dictionary<string, string>();
    }
}