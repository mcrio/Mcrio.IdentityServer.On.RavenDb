using System.Collections.Generic;
using IdentityModel;
using IdentityServer4.Models;

namespace Mcrio.IdentityServer.On.RavenDb.Sample.IdentityServer
{
    internal static class TestData
    {
        // Having ApiResources populates the `aud` token value with APIs that have the requested scope.
        internal static IEnumerable<ApiResource> GetApiResources() => new List<ApiResource>
        {
            new ApiResource("my_api", "My Api")
            {
                Scopes = { "my_api.access", "shared_scope" }
            },
            new ApiResource("other_api", "Some Other Api")
            {
                Scopes = { "other_api.access", "shared_scope" }
            },
        };
        
        internal static IEnumerable<ApiScope> GetScopes() => new[]
        {
            new ApiScope("my_api.access", "Access to MyApi"),
            new ApiScope("shared_scope", "Some shared scope"),
            new ApiScope("other_api.access", "Access to Other Api"), 
        };

        internal static IEnumerable<Client> GetClients() => new List<Client>
        {
            new Client
            {
                ClientId = "machine_to_machine",
                ClientSecrets = { new Secret("machine_to_machine_secret".ToSha256()) },
                AllowedGrantTypes = GrantTypes.ClientCredentials,
                AllowedScopes = { "my_api.access", "shared_scope" }
            }
        };
    }
}