using System;
using System.Threading.Tasks;
using FluentAssertions;
using Mcrio.IdentityServer.On.RavenDb.Storage.Entities;
using Raven.Client.Documents;
using Xunit;

namespace Mcrio.IdentityServer.On.RavenDb.Storage.Tests.IntegrationTests.TokenCleanup
{
    public class TokenCleanupServiceTest : IntegrationTestBase
    {
        [Fact]
        public async Task RemoveExpiredGrantsAsync_WhenExpiredGrantsExist_ExpectExpiredGrantsRemoved()
        {
            var expiredGrant = new PersistedGrant
            {
                Key = Guid.NewGuid().ToString(),
                ClientId = "app1",
                Type = "reference",
                SubjectId = "123",
                Expiration = DateTime.UtcNow.AddDays(-3),
                Data = "{!}",
            };

            ServiceScope scope = InitializeServices();
            await scope.DocumentSession.StoreAsync(expiredGrant);
            await scope.DocumentSession.SaveChangesAsync();

            await InitializeServices().TokenCleanupService.RemoveExpiredGrantsAsync();

            PersistedGrant? databaseItem = await InitializeServices()
                .DocumentSession
                .Query<PersistedGrant>()
                .FirstOrDefaultAsync(x => x.Key == expiredGrant.Key);

            databaseItem.Should().BeNull();
        }

        [Fact]
        public async Task RemoveExpiredGrantsAsync_WhenValidGrantsExist_ExpectValidGrantsInDb()
        {
            var validGrant = new PersistedGrant
            {
                Key = Guid.NewGuid().ToString(),
                ClientId = "app1",
                Type = "reference",
                SubjectId = "123",
                Expiration = DateTime.UtcNow.AddDays(3),
                Data = "{!}",
            };

            ServiceScope scope = InitializeServices();
            await scope.DocumentSession.StoreAsync(validGrant);
            await scope.DocumentSession.SaveChangesAsync();

            await InitializeServices().TokenCleanupService.RemoveExpiredGrantsAsync();

            PersistedGrant? databaseItem = await InitializeServices()
                .DocumentSession
                .Query<PersistedGrant>()
                .FirstOrDefaultAsync(x => x.Key == validGrant.Key);

            databaseItem.Should().NotBeNull();
        }

        [Fact]
        public async Task RemoveExpiredGrantsAsync_WhenExpiredDeviceGrantsExist_ExpectExpiredDeviceGrantsRemoved()
        {
            var expiredGrant = new DeviceFlowCode
            {
                DeviceCode = Guid.NewGuid().ToString(),
                UserCode = Guid.NewGuid().ToString(),
                ClientId = "app1",
                SubjectId = "123",
                CreationTime = DateTime.UtcNow.AddDays(-4),
                Expiration = DateTime.UtcNow.AddDays(-3),
                Data = "{!}",
            };

            ServiceScope scope = InitializeServices();
            await scope.DocumentSession.StoreAsync(expiredGrant);
            await scope.DocumentSession.SaveChangesAsync();

            await InitializeServices().TokenCleanupService.RemoveExpiredGrantsAsync();

            DeviceFlowCode? databaseItem = await InitializeServices()
                .DocumentSession
                .Query<DeviceFlowCode>()
                .FirstOrDefaultAsync(x => x.DeviceCode == expiredGrant.DeviceCode);

            databaseItem.Should().BeNull();
        }

        [Fact]
        public async Task RemoveExpiredGrantsAsync_WhenValidDeviceGrantsExist_ExpectValidDeviceGrantsInDb()
        {
            var validGrant = new DeviceFlowCode
            {
                DeviceCode = Guid.NewGuid().ToString(),
                UserCode = "2468",
                ClientId = "app1",
                SubjectId = "123",
                CreationTime = DateTime.UtcNow.AddDays(-4),
                Expiration = DateTime.UtcNow.AddDays(3),
                Data = "{!}",
            };

            ServiceScope scope = InitializeServices();
            await scope.DocumentSession.StoreAsync(validGrant);
            await scope.DocumentSession.SaveChangesAsync();

            await InitializeServices().TokenCleanupService.RemoveExpiredGrantsAsync();

            DeviceFlowCode? databaseItem = await InitializeServices()
                .DocumentSession
                .Query<DeviceFlowCode>()
                .FirstOrDefaultAsync(x => x.DeviceCode == validGrant.DeviceCode);

            databaseItem.Should().NotBeNull();
        }
    }
}