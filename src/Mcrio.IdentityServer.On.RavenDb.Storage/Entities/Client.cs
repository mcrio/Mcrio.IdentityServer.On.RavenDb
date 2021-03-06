using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using IdentityServer4.Models;

#pragma warning disable 8618
#pragma warning disable 1591

namespace Mcrio.IdentityServer.On.RavenDb.Storage.Entities
{
    /// <summary>
    /// IDS4 Client.
    /// </summary>
    [SuppressMessage("ReSharper", "SA1600", Justification = "Check IDS4 documentation for property descriptions.")]
    public class Client : IEntity
    {
        public string Id { get; set; }

        public bool Enabled { get; set; } = true;

        public string ProtocolType { get; set; } = "oidc";

        public List<ClientSecret> ClientSecrets { get; set; } = new List<ClientSecret>();

        public bool RequireClientSecret { get; set; } = true;

        public string ClientId { get; set; }

        public string ClientName { get; set; }

        public string Description { get; set; }

        public string ClientUri { get; set; }

        public string LogoUri { get; set; }

        public bool RequireConsent { get; set; } = false;

        public bool AllowRememberConsent { get; set; } = true;

        public bool AlwaysIncludeUserClaimsInIdToken { get; set; }

        public List<string> AllowedGrantTypes { get; set; } = new List<string>();

        public bool RequirePkce { get; set; } = true;

        public bool AllowPlainTextPkce { get; set; }

        public bool RequireRequestObject { get; set; }

        public bool AllowAccessTokensViaBrowser { get; set; }

        public List<string> RedirectUris { get; set; } = new List<string>();

        public List<string> PostLogoutRedirectUris { get; set; } = new List<string>();

        public string FrontChannelLogoutUri { get; set; }

        public bool FrontChannelLogoutSessionRequired { get; set; } = true;

        public string BackChannelLogoutUri { get; set; }

        public bool BackChannelLogoutSessionRequired { get; set; } = true;

        public bool AllowOfflineAccess { get; set; }

        public List<string> AllowedScopes { get; set; } = new List<string>();

        public int IdentityTokenLifetime { get; set; } = 300;

        public List<string> AllowedIdentityTokenSigningAlgorithms { get; set; } = new List<string>();

        public int AccessTokenLifetime { get; set; } = 3600;

        public int AuthorizationCodeLifetime { get; set; } = 300;

        public int? ConsentLifetime { get; set; } = null;

        public int AbsoluteRefreshTokenLifetime { get; set; } = 2592000;

        public int SlidingRefreshTokenLifetime { get; set; } = 1296000;

        public int RefreshTokenUsage { get; set; } = (int)TokenUsage.OneTimeOnly;

        public bool UpdateAccessTokenClaimsOnRefresh { get; set; }

        public int RefreshTokenExpiration { get; set; } = (int)TokenExpiration.Absolute;

        public int AccessTokenType { get; set; } = (int)0; // AccessTokenType.Jwt;

        public bool EnableLocalLogin { get; set; } = true;

        public List<string> IdentityProviderRestrictions { get; set; } = new List<string>();

        public bool IncludeJwtId { get; set; }

        public List<ClientClaim> Claims { get; set; } = new List<ClientClaim>();

        public bool AlwaysSendClientClaims { get; set; }

        public string ClientClaimsPrefix { get; set; } = "client_";

        public string PairWiseSubjectSalt { get; set; }

        public List<string> AllowedCorsOrigins { get; set; } = new List<string>();

        public Dictionary<string, string> Properties { get; set; } = new Dictionary<string, string>();

        public DateTime Created { get; set; } = DateTime.UtcNow;

        public DateTime? Updated { get; set; }

        public DateTime? LastAccessed { get; set; }

        public int? UserSsoLifetime { get; set; }

        public string UserCodeType { get; set; }

        public int DeviceCodeLifetime { get; set; } = 300;

        public bool NonEditable { get; set; }
    }
}