using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using IdentityServer4.Models;
using Xunit;

namespace Mcrio.IdentityServer.On.RavenDb.Storage.Tests.IntegrationTests.Cors
{
    public class CorsPolicyServiceTest : IntegrationTestBase
    {
        [Fact]
        public async Task IsOriginAllowedAsync_WhenOriginIsAllowed_ExpectTrue()
        {
            const string testCorsOrigin = "https://identityserver.io/";

            await InitializeServices().ClientStoreAdditions.CreateAsync(new Client
            {
                ClientId = Guid.NewGuid().ToString(),
                ClientName = Guid.NewGuid().ToString(),
                AllowedCorsOrigins = new List<string> { "https://www.identityserver.com" },
            });

            await InitializeServices().ClientStoreAdditions.CreateAsync(new Client
            {
                ClientId = "2",
                ClientName = "2",
                AllowedCorsOrigins = new List<string> { "https://www.identityserver.com", testCorsOrigin },
            });

            bool corsIsAllowed = await InitializeServices().CorsPolicyService.IsOriginAllowedAsync(testCorsOrigin);
            Assert.True(corsIsAllowed);

            WaitForUserToContinueTheTest(InitializeServices().DocumentStore);
        }

        [Fact]
        public async Task IsOriginAllowedAsync_WhenOriginIsNotAllowed_ExpectFalse()
        {
            await InitializeServices().ClientStoreAdditions.CreateAsync(new Client
            {
                ClientId = Guid.NewGuid().ToString(),
                ClientName = Guid.NewGuid().ToString(),
                AllowedCorsOrigins = new List<string> { "https://www.identityserver.com" },
            });

            bool corsIsAllowed = await InitializeServices().CorsPolicyService.IsOriginAllowedAsync("InvalidOrigin");
            Assert.False(corsIsAllowed);
        }
    }
}