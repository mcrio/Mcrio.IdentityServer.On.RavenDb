using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using FluentAssertions;
using IdentityModel;
using IdentityServer4.Models;
using IdentityServer4.Stores.Serialization;
using Mcrio.IdentityServer.On.RavenDb.Storage.Entities;
using Mcrio.IdentityServer.On.RavenDb.Storage.Stores.Exceptions;
using Raven.Client;
using Raven.Client.Documents;
using Raven.Client.Documents.Session;
using Xunit;

namespace Mcrio.IdentityServer.On.RavenDb.Storage.Tests.IntegrationTests.Stores
{
    public class DeviceFlowStoreTest : IntegrationTestBase
    {
        private readonly IPersistentGrantSerializer _serializer = new PersistentGrantSerializer();

        [Fact]
        public async Task StoreDeviceAuthorizationAsync_WhenSuccessful_ExpectDeviceCodeAndUserCodeStored()
        {
            var deviceCode = Guid.NewGuid().ToString();
            var userCode = Guid.NewGuid().ToString();
            var data = new DeviceCode
            {
                ClientId = Guid.NewGuid().ToString(),
                CreationTime = DateTime.UtcNow,
                Lifetime = 300,
            };
            await InitializeServices().DeviceFlowStore.StoreDeviceAuthorizationAsync(deviceCode, userCode, data);

            {
                ServiceScope scope = InitializeServices();
                IAsyncDocumentSession session = scope.DocumentSession;
                WaitForIndexing(scope.DocumentStore);
                DeviceFlowCode foundDeviceFlowCodes = await session
                    .Query<DeviceFlowCode>()
                    .SingleOrDefaultAsync(flowCode => flowCode.DeviceCode == deviceCode);

                foundDeviceFlowCodes.Should().NotBeNull();
                foundDeviceFlowCodes.DeviceCode.Should().Be(deviceCode);
                foundDeviceFlowCodes.UserCode.Should().Be(userCode);
            }

            await AssertCompareExchangeKeyExistsWithValueAsync(
                $"idsrv/devcode/{deviceCode}",
                InitializeServices().Mapper.CreateEntityId<DeviceFlowCode>(userCode),
                "device code should be unique so we store it in the RavenDb compare exchange."
            );
        }

        [Fact]
        public async Task StoreDeviceAuthorizationAsync_WhenSuccessful_ExpectDataStored()
        {
            var deviceCode = Guid.NewGuid().ToString();
            var userCode = Guid.NewGuid().ToString();
            var data = new DeviceCode
            {
                ClientId = Guid.NewGuid().ToString(),
                CreationTime = DateTime.UtcNow,
                Lifetime = 300,
            };
            await InitializeServices().DeviceFlowStore.StoreDeviceAuthorizationAsync(deviceCode, userCode, data);

            {
                ServiceScope scope = InitializeServices();
                IAsyncDocumentSession session = scope.DocumentSession;
                WaitForIndexing(scope.DocumentStore);
                DeviceFlowCode foundDeviceFlowCodes = await session
                    .Query<DeviceFlowCode>()
                    .SingleOrDefaultAsync(flowCode => flowCode.DeviceCode == deviceCode);

                foundDeviceFlowCodes.Should().NotBeNull();
                DeviceCode deserializedData =
                    new PersistentGrantSerializer().Deserialize<DeviceCode>(foundDeviceFlowCodes?.Data);

                deserializedData.CreationTime.Should().BeCloseTo(data.CreationTime);
                deserializedData.ClientId.Should().Be(data.ClientId);
                deserializedData.Lifetime.Should().Be(data.Lifetime);
            }

            await AssertCompareExchangeKeyExistsWithValueAsync(
                $"idsrv/devcode/{deviceCode}",
                InitializeServices().Mapper.CreateEntityId<DeviceFlowCode>(userCode),
                "device code should be unique so we store it in the RavenDb compare exchange."
            );
        }

        [Fact]
        public async Task StoreDeviceAuthorizationAsync_WhenUserCodeAlreadyExists_ExpectException()
        {
            var existingUserCode = $"user_{Guid.NewGuid().ToString()}";
            var deviceCodeData = new DeviceCode
            {
                ClientId = "device_flow",
                RequestedScopes = new[] { "openid", "api1" },
                CreationTime = new DateTime(2018, 10, 19, 16, 14, 29),
                Lifetime = 300,
                IsOpenId = true,
                Subject = new ClaimsPrincipal(new ClaimsIdentity(
                    new List<Claim> { new Claim(JwtClaimTypes.Subject, $"sub_{Guid.NewGuid().ToString()}") })
                ),
            };

            await InitializeServices().DeviceFlowStore.StoreDeviceAuthorizationAsync(
                $"device_{Guid.NewGuid().ToString()}",
                existingUserCode,
                deviceCodeData
            );

            var anotherDeviceCode = $"device_{Guid.NewGuid().ToString()}";
            Func<Task> addDuplicate = async () =>
            {
                await InitializeServices().DeviceFlowStore.StoreDeviceAuthorizationAsync(
                    anotherDeviceCode,
                    existingUserCode,
                    deviceCodeData
                );
            };

            await addDuplicate.Should().ThrowAsync<DuplicateException>();

            await AssertCompareExchangeKeyDoesNotExistAsync(
                $"identityserver/devicecode/{anotherDeviceCode}",
                "entity with same user code was not created"
            );
        }

        [Fact]
        public async Task StoreDeviceAuthorizationAsync_WhenDeviceCodeAlreadyExists_ExpectException()
        {
            var existingDeviceCode = $"device_{Guid.NewGuid().ToString()}";
            var deviceCodeData = new DeviceCode
            {
                ClientId = "device_flow",
                RequestedScopes = new[] { "openid", "api1" },
                CreationTime = new DateTime(2018, 10, 19, 16, 14, 29),
                Lifetime = 300,
                IsOpenId = true,
                Subject = new ClaimsPrincipal(new ClaimsIdentity(
                    new List<Claim> { new Claim(JwtClaimTypes.Subject, $"sub_{Guid.NewGuid().ToString()}") }
                )),
            };

            var firstDeviceUserCode = $"user_{Guid.NewGuid().ToString()}";
            await InitializeServices().DeviceFlowStore.StoreDeviceAuthorizationAsync(
                existingDeviceCode,
                firstDeviceUserCode,
                deviceCodeData
            );

            Func<Task> addDuplicate = async () =>
            {
                await InitializeServices().DeviceFlowStore.StoreDeviceAuthorizationAsync(
                    existingDeviceCode,
                    $"user_{Guid.NewGuid().ToString()}",
                    deviceCodeData
                );
            };

            await addDuplicate.Should().ThrowAsync<DuplicateException>();

            await AssertCompareExchangeKeyExistsWithValueAsync(
                $"idsrv/devcode/{existingDeviceCode}",
                InitializeServices().Mapper.CreateEntityId<DeviceFlowCode>(firstDeviceUserCode),
                "device code should exist as it was reserved by the first device"
            );
        }

        [Fact]
        public async Task FindByUserCodeAsync_WhenUserCodeExists_ExpectDataRetrievedCorrectly()
        {
            var testDeviceCode = $"device_{Guid.NewGuid().ToString()}";
            var testUserCode = $"user_{Guid.NewGuid().ToString()}";

            var expectedSubject = $"sub_{Guid.NewGuid().ToString()}";
            var expectedDeviceCodeData = new DeviceCode
            {
                ClientId = "device_flow",
                RequestedScopes = new[] { "openid", "api1" },
                CreationTime = new DateTime(2018, 10, 19, 16, 14, 29),
                Lifetime = 300,
                IsOpenId = true,
                Subject = new ClaimsPrincipal(new ClaimsIdentity(new List<Claim>
                    { new Claim(JwtClaimTypes.Subject, expectedSubject) }
                )),
            };

            await InitializeServices().DeviceFlowStore.StoreDeviceAuthorizationAsync(
                testDeviceCode,
                testUserCode,
                expectedDeviceCodeData
            );

            DeviceCode code = await InitializeServices().DeviceFlowStore.FindByUserCodeAsync(testUserCode);

            code.Should().BeEquivalentTo(
                expectedDeviceCodeData,
                assertionOptions => assertionOptions.Excluding(x => x.Subject)
            );

            code.Subject
                .Claims
                .FirstOrDefault(x => x.Type == JwtClaimTypes.Subject && x.Value == expectedSubject)
                .Should()
                .NotBeNull();
        }

        [Fact]
        public async Task FindByUserCodeAsync_WhenUserCodeDoesNotExist_ExpectNull()
        {
            DeviceCode? code = await InitializeServices()
                .DeviceFlowStore
                .FindByUserCodeAsync($"user_{Guid.NewGuid().ToString()}");
            code.Should().BeNull();
        }

        [Fact]
        public async Task FindByDeviceCodeAsync_WhenDeviceCodeExists_ExpectDataRetrievedCorrectly()
        {
            var testDeviceCode = $"device_{Guid.NewGuid().ToString()}";
            var testUserCode = $"user_{Guid.NewGuid().ToString()}";

            var expectedSubject = $"sub_{Guid.NewGuid().ToString()}";
            var expectedDeviceCodeData = new DeviceCode
            {
                ClientId = "device_flow",
                RequestedScopes = new[] { "openid", "api1" },
                CreationTime = new DateTime(2018, 10, 19, 16, 14, 29),
                Lifetime = 300,
                IsOpenId = true,
                Subject = new ClaimsPrincipal(new ClaimsIdentity(new List<Claim>
                    { new Claim(JwtClaimTypes.Subject, expectedSubject) }
                )),
            };

            await InitializeServices().DeviceFlowStore.StoreDeviceAuthorizationAsync(
                testDeviceCode,
                testUserCode,
                expectedDeviceCodeData
            );

            DeviceCode code = await InitializeServices().DeviceFlowStore.FindByDeviceCodeAsync(testDeviceCode);

            code.Should().BeEquivalentTo(
                expectedDeviceCodeData,
                assertionOptions => assertionOptions.Excluding(x => x.Subject)
            );

            code.Subject
                .Claims
                .FirstOrDefault(x => x.Type == JwtClaimTypes.Subject && x.Value == expectedSubject)
                .Should()
                .NotBeNull();
        }

        [Fact]
        public async Task FindByDeviceCodeAsync_WhenDeviceCodeDoesNotExist_ExpectNull()
        {
            var code = await InitializeServices()
                .DeviceFlowStore
                .FindByDeviceCodeAsync($"device_{Guid.NewGuid().ToString()}");
            code.Should().BeNull();
        }

        [Fact]
        public async Task UpdateByUserCodeAsync_WhenDeviceCodeAuthorized_ExpectSubjectAndDataUpdated()
        {
            var testDeviceCode = $"device_{Guid.NewGuid().ToString()}";
            var testUserCode = $"user_{Guid.NewGuid().ToString()}";

            var expectedSubject = $"sub_{Guid.NewGuid().ToString()}";
            var unauthorizedDeviceCode = new DeviceCode
            {
                ClientId = "device_flow",
                RequestedScopes = new[] { "openid", "api1" },
                CreationTime = new DateTime(2018, 10, 19, 16, 14, 29),
                Lifetime = 300,
                IsOpenId = true,
            };

            await InitializeServices().DeviceFlowStore.StoreDeviceAuthorizationAsync(
                testDeviceCode,
                testUserCode,
                unauthorizedDeviceCode
            );

            var authorizedDeviceCode = new DeviceCode
            {
                ClientId = unauthorizedDeviceCode.ClientId,
                RequestedScopes = unauthorizedDeviceCode.RequestedScopes,
                AuthorizedScopes = unauthorizedDeviceCode.RequestedScopes,
                Subject = new ClaimsPrincipal(new ClaimsIdentity(new List<Claim>
                    { new Claim(JwtClaimTypes.Subject, expectedSubject) })),
                IsAuthorized = true,
                IsOpenId = true,
                CreationTime = new DateTime(2018, 10, 19, 16, 14, 29),
                Lifetime = 300,
            };

            await InitializeServices().DeviceFlowStore.UpdateByUserCodeAsync(testUserCode, authorizedDeviceCode);

            DeviceFlowCode updatedCode;

            {
                ServiceScope scope = InitializeServices();
                IAsyncDocumentSession session = scope.DocumentSession;
                WaitForIndexing(scope.DocumentStore);
                updatedCode = await session
                    .Query<DeviceFlowCode>()
                    .SingleAsync(x => x.UserCode == testUserCode);
            }

            // should be unchanged
            updatedCode.DeviceCode.Should().Be(testDeviceCode);
            updatedCode.ClientId.Should().Be(unauthorizedDeviceCode.ClientId);
            updatedCode.CreationTime.Should().Be(unauthorizedDeviceCode.CreationTime);
            updatedCode.Expiration
                .Should()
                .Be(unauthorizedDeviceCode.CreationTime.AddSeconds(authorizedDeviceCode.Lifetime));

            // should be changed
            var parsedCode = _serializer.Deserialize<DeviceCode>(updatedCode.Data);
            parsedCode.Should().BeEquivalentTo(
                authorizedDeviceCode,
                assertionOptions => assertionOptions.Excluding(x => x.Subject)
            );
            parsedCode.Subject
                .Claims
                .FirstOrDefault(x => x.Type == JwtClaimTypes.Subject && x.Value == expectedSubject)
                .Should().NotBeNull();
        }

        [Fact]
        public async Task RemoveByDeviceCodeAsync_WhenDeviceCodeExists_ExpectDeviceCodeDeleted()
        {
            var testDeviceCode = $"device_{Guid.NewGuid().ToString()}";
            var testUserCode = $"user_{Guid.NewGuid().ToString()}";

            var existingDeviceCode = new DeviceCode
            {
                ClientId = "device_flow",
                RequestedScopes = new[] { "openid", "api1" },
                CreationTime = new DateTime(2018, 10, 19, 16, 14, 29),
                Lifetime = 300,
                IsOpenId = true,
            };

            await InitializeServices().DeviceFlowStore.StoreDeviceAuthorizationAsync(
                testDeviceCode,
                testUserCode,
                existingDeviceCode
            );

            await AssertCompareExchangeKeyExistsWithValueAsync(
                $"idsrv/devcode/{testDeviceCode}",
                InitializeServices().Mapper.CreateEntityId<Entities.DeviceFlowCode>(testUserCode)
            );

            await InitializeServices().DeviceFlowStore.RemoveByDeviceCodeAsync(testDeviceCode);

            {
                ServiceScope scope = InitializeServices();
                IAsyncDocumentSession session = scope.DocumentSession;
                WaitForIndexing(scope.DocumentStore);
                DeviceFlowCode foundCode = await session
                    .Query<DeviceFlowCode>()
                    .SingleOrDefaultAsync(x => x.UserCode == testUserCode);
                foundCode.Should().BeNull();
            }

            await AssertCompareExchangeKeyDoesNotExistAsync($"identityserver/devicecode/{testDeviceCode}");
        }

        [Fact]
        public async Task RemoveByDeviceCodeAsync_WhenDeviceCodeDoesNotExists_ExpectSuccess()
        {
            var deviceCode = $"device_{Guid.NewGuid().ToString()}";
            await InitializeServices().DeviceFlowStore.RemoveByDeviceCodeAsync(deviceCode);
            await AssertCompareExchangeKeyDoesNotExistAsync($"identityserver/devicecode/{deviceCode}");
        }

        [Fact]
        public async Task ShouldAddDocumentExpiresMetadataOnCreateAndUpdateIfOptionEnabled()
        {
            var testDeviceCode = $"device_{Guid.NewGuid().ToString()}";
            var testUserCode = $"user_{Guid.NewGuid().ToString()}";

            var deviceCode = new DeviceCode
            {
                ClientId = "device_flow",
                RequestedScopes = new[] { "openid", "api1" },
                CreationTime = new DateTime(2018, 10, 19, 16, 14, 29),
                Lifetime = 300,
                IsOpenId = true,
                Subject = new ClaimsPrincipal(new ClaimsIdentity(new List<Claim>
                    { new Claim(JwtClaimTypes.Subject, $"sub_{Guid.NewGuid().ToString()}") }
                )),
            };

            await InitializeServices(options => { options.SetRavenDbDocumentExpiresMetadata = true; }).DeviceFlowStore
                .StoreDeviceAuthorizationAsync(
                    testDeviceCode,
                    testUserCode,
                    deviceCode
                );

            {
                ServiceScope scope = InitializeServices();
                IAsyncDocumentSession session = scope.DocumentSession;
                WaitForIndexing(scope.DocumentStore);
                DeviceFlowCode foundCode = await session
                    .Query<DeviceFlowCode>()
                    .SingleOrDefaultAsync(x => x.UserCode == testUserCode);
                foundCode.Should().NotBeNull();

                IMetadataDictionary metadata = scope.DocumentSession.Advanced.GetMetadataFor(foundCode);
                metadata.Should().ContainKey(Constants.Documents.Metadata.Expires);
                metadata[Constants.Documents.Metadata.Expires]
                    .Should()
                    .Be(deviceCode.CreationTime.AddSeconds(deviceCode.Lifetime).ToUniversalTime().ToString("O"));
            }

            deviceCode.Description = "Updated description";
            await InitializeServices(
                options => { options.SetRavenDbDocumentExpiresMetadata = false; }
            ).DeviceFlowStore.UpdateByUserCodeAsync(testUserCode, deviceCode);

            {
                ServiceScope scope = InitializeServices();
                IAsyncDocumentSession session = scope.DocumentSession;
                WaitForIndexing(scope.DocumentStore);
                DeviceFlowCode foundCode = await session
                    .Query<DeviceFlowCode>()
                    .SingleOrDefaultAsync(x => x.UserCode == testUserCode);
                foundCode.Should().NotBeNull();

                IMetadataDictionary metadata = scope.DocumentSession.Advanced.GetMetadataFor(foundCode);
                metadata.Should().ContainKey(Constants.Documents.Metadata.Expires);
                metadata[Constants.Documents.Metadata.Expires]
                    .Should()
                    .Be(deviceCode.CreationTime.AddSeconds(deviceCode.Lifetime).ToUniversalTime().ToString("O"));
            }
        }

        [Fact]
        public async Task ShouldNotAddDocumentExpiresMetadataOnCreateAndUpdateIfOptionDisabled()
        {
            var testDeviceCode = $"device_{Guid.NewGuid().ToString()}";
            var testUserCode = $"user_{Guid.NewGuid().ToString()}";

            var deviceCode = new DeviceCode
            {
                ClientId = "device_flow",
                RequestedScopes = new[] { "openid", "api1" },
                CreationTime = new DateTime(2018, 10, 19, 16, 14, 29),
                Lifetime = 300,
                IsOpenId = true,
                Subject = new ClaimsPrincipal(new ClaimsIdentity(new List<Claim>
                    { new Claim(JwtClaimTypes.Subject, $"sub_{Guid.NewGuid().ToString()}") }
                )),
            };

            await InitializeServices(
                    options => { options.SetRavenDbDocumentExpiresMetadata = false; }
                )
                .DeviceFlowStore
                .StoreDeviceAuthorizationAsync(
                    testDeviceCode,
                    testUserCode,
                    deviceCode
                );

            {
                ServiceScope scope = InitializeServices();
                IAsyncDocumentSession session = scope.DocumentSession;
                WaitForIndexing(scope.DocumentStore);
                DeviceFlowCode foundCode = await session
                    .Query<DeviceFlowCode>()
                    .SingleOrDefaultAsync(x => x.UserCode == testUserCode);
                foundCode.Should().NotBeNull();

                IMetadataDictionary metadata = scope.DocumentSession.Advanced.GetMetadataFor(foundCode);
                metadata.Should().NotContainKey(Constants.Documents.Metadata.Expires, "feature is disabled");
            }

            deviceCode.Description = "Updated description";
            await InitializeServices(
                options => { options.SetRavenDbDocumentExpiresMetadata = false; }
            ).DeviceFlowStore.UpdateByUserCodeAsync(testUserCode, deviceCode);

            {
                ServiceScope scope = InitializeServices();
                IAsyncDocumentSession session = scope.DocumentSession;
                WaitForIndexing(scope.DocumentStore);
                DeviceFlowCode foundCode = await session
                    .Query<DeviceFlowCode>()
                    .SingleOrDefaultAsync(x => x.UserCode == testUserCode);
                foundCode.Should().NotBeNull();

                IMetadataDictionary metadata = scope.DocumentSession.Advanced.GetMetadataFor(foundCode);
                metadata.Should().NotContainKey(Constants.Documents.Metadata.Expires, "feature is disabled");
            }
        }
    }
}