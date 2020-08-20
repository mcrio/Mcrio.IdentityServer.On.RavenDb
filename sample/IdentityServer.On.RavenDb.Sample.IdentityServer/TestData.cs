using System.Collections.Generic;
using IdentityModel;
using IdentityServer4.Models;

namespace Mcrio.IdentityServer.On.RavenDb.Sample.IdentityServer
{
    internal static class TestData
    {
        internal static IEnumerable<ApiScope> GetScopes() => new[]
        {
            new ApiScope("my_api", "My Api Scope"),
        };
        
        // Will populate the `aud` property with APIs that have the requested scope.
        internal static IEnumerable<ApiResource> GetApis() => new List<ApiResource>
        {
            // new ApiResource("my_api", "My Api")
            // {
            //     Scopes = { "my_api" }
            // },
            new ApiResource("another_api", "Some other api")
            {
                Scopes = { "my_api" }
            },
        };

        internal static IEnumerable<Client> GetClients() => new List<Client>
        {
            new Client
            {
                ClientId = "my_client",
                ClientSecrets = { new Secret("my_secret".ToSha256()) },
                AllowedGrantTypes = GrantTypes.ClientCredentials,
                AllowedScopes = { "my_api" }
            }
        };
    }
}