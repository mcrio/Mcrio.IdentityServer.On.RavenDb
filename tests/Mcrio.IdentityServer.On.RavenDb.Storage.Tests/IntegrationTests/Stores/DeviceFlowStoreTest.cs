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
            await NewServiceScope().DeviceFlowStore.StoreDeviceAuthorizationAsync(deviceCode, userCode, data);

            {
                ServiceScope scope = NewServiceScope();
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
                NewServiceScope().Mapper.CreateEntityId<DeviceFlowCode>(userCode),
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
            await NewServiceScope().DeviceFlowStore.StoreDeviceAuthorizationAsync(deviceCode, userCode, data);

            {
                ServiceScope scope = NewServiceScope();
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
                NewServiceScope().Mapper.CreateEntityId<DeviceFlowCode>(userCode),
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
                CreationTime = DateTime.Now,
                Lifetime = 300,
                IsOpenId = true,
                Subject = new ClaimsPrincipal(new ClaimsIdentity(
                    new List<Claim> { new Claim(JwtClaimTypes.Subject, $"sub_{Guid.NewGuid().ToString()}") })
                ),
            };

            var scope = NewServiceScope();
            await scope.DeviceFlowStore.StoreDeviceAuthorizationAsync(
                $"device_{Guid.NewGuid().ToString()}",
                existingUserCode,
                deviceCodeData
            );

            WaitForUserToContinueTheTest(scope.DocumentStore);

            var anotherDeviceCode = $"device_{Guid.NewGuid().ToString()}";
            Func<Task> addDuplicate = async () =>
            {
                await NewServiceScope().DeviceFlowStore.StoreDeviceAuthorizationAsync(
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
                CreationTime = DateTime.Now,
                Lifetime = 300,
                IsOpenId = true,
                Subject = new ClaimsPrincipal(new ClaimsIdentity(
                    new List<Claim> { new Claim(JwtClaimTypes.Subject, $"sub_{Guid.NewGuid().ToString()}") }
                )),
            };

            var firstDeviceUserCode = $"user_{Guid.NewGuid().ToString()}";
            await NewServiceScope().DeviceFlowStore.StoreDeviceAuthorizationAsync(
                existingDeviceCode,
                firstDeviceUserCode,
                deviceCodeData
            );

            Func<Task> addDuplicate = async () =>
            {
                await NewServiceScope().DeviceFlowStore.StoreDeviceAuthorizationAsync(
                    existingDeviceCode,
                    $"user_{Guid.NewGuid().ToString()}",
                    deviceCodeData
                );
            };

            await addDuplicate.Should().ThrowAsync<DuplicateException>();

            await AssertCompareExchangeKeyExistsWithValueAsync(
                $"idsrv/devcode/{existingDeviceCode}",
                NewServiceScope().Mapper.CreateEntityId<DeviceFlowCode>(firstDeviceUserCode),
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
                CreationTime = DateTime.Now,
                Lifetime = 300,
                IsOpenId = true,
                Subject = new ClaimsPrincipal(new ClaimsIdentity(new List<Claim>
                    { new Claim(JwtClaimTypes.Subject, expectedSubject) }
                )),
            };

            await NewServiceScope().DeviceFlowStore.StoreDeviceAuthorizationAsync(
                testDeviceCode,
                testUserCode,
                expectedDeviceCodeData
            );

            DeviceCode code = await NewServiceScope().DeviceFlowStore.FindByUserCodeAsync(testUserCode);

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
            DeviceCode? code = await NewServiceScope()
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
                CreationTime = DateTime.Now,
                Lifetime = 300,
                IsOpenId = true,
                Subject = new ClaimsPrincipal(new ClaimsIdentity(new List<Claim>
                    { new Claim(JwtClaimTypes.Subject, expectedSubject) }
                )),
            };

            await NewServiceScope().DeviceFlowStore.StoreDeviceAuthorizationAsync(
                testDeviceCode,
                testUserCode,
                expectedDeviceCodeData
            );

            DeviceCode code = await NewServiceScope().DeviceFlowStore.FindByDeviceCodeAsync(testDeviceCode);

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
            var code = await NewServiceScope()
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
                CreationTime = DateTime.Now,
                Lifetime = 300,
                IsOpenId = true,
            };

            await NewServiceScope().DeviceFlowStore.StoreDeviceAuthorizationAsync(
                testDeviceCode,
                testUserCode,
                unauthorizedDeviceCode
            );

            WaitForUserToContinueTheTest(NewServiceScope().DocumentStore);

            var authorizedDeviceCode = new DeviceCode
            {
                ClientId = unauthorizedDeviceCode.ClientId,
                RequestedScopes = unauthorizedDeviceCode.RequestedScopes,
                AuthorizedScopes = unauthorizedDeviceCode.RequestedScopes,
                Subject = new ClaimsPrincipal(new ClaimsIdentity(new List<Claim>
                    { new Claim(JwtClaimTypes.Subject, expectedSubject) })),
                IsAuthorized = true,
                IsOpenId = true,
                CreationTime = DateTime.Now,
                Lifetime = 600,
            };

            await NewServiceScope().DeviceFlowStore.UpdateByUserCodeAsync(testUserCode, authorizedDeviceCode);

            WaitForUserToContinueTheTest(NewServiceScope().DocumentStore);

            DeviceFlowCode updatedCode;

            {
                ServiceScope scope = NewServiceScope();
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
                .Be(unauthorizedDeviceCode.CreationTime.AddSeconds(unauthorizedDeviceCode.Lifetime));

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
                CreationTime = DateTime.Now,
                Lifetime = 300,
                IsOpenId = true,
            };

            await NewServiceScope().DeviceFlowStore.StoreDeviceAuthorizationAsync(
                testDeviceCode,
                testUserCode,
                existingDeviceCode
            );

            await AssertCompareExchangeKeyExistsWithValueAsync(
                $"idsrv/devcode/{testDeviceCode}",
                NewServiceScope().Mapper.CreateEntityId<DeviceFlowCode>(testUserCode)
            );

            await NewServiceScope().DeviceFlowStore.RemoveByDeviceCodeAsync(testDeviceCode);

            {
                ServiceScope scope = NewServiceScope();
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
            await NewServiceScope().DeviceFlowStore.RemoveByDeviceCodeAsync(deviceCode);
            await AssertCompareExchangeKeyDoesNotExistAsync($"identityserver/devicecode/{deviceCode}");
        }

        [Fact]
        public async Task ShouldAddDocumentExpiresMetadataOnCreateAndUpdateIfOptionEnabled()
        {
            var testDeviceCode = $"device_{Guid.NewGuid().ToString()}";
            var testUserCode = $"user_{Guid.NewGuid().ToString()}";

            var originalDeviceCode = new DeviceCode
            {
                ClientId = "device_flow",
                RequestedScopes = new[] { "openid", "api1" },
                CreationTime = DateTime.Now,
                Lifetime = 300,
                IsOpenId = true,
                Subject = new ClaimsPrincipal(new ClaimsIdentity(new List<Claim>
                    { new Claim(JwtClaimTypes.Subject, $"sub_{Guid.NewGuid().ToString()}") }
                )),
            };

            await NewServiceScope(
                    options => { options.SetRavenDbDocumentExpiresMetadata = true; }
                ).DeviceFlowStore
                .StoreDeviceAuthorizationAsync(
                    testDeviceCode,
                    testUserCode,
                    originalDeviceCode
                );

            {
                ServiceScope scope = NewServiceScope();
                IAsyncDocumentSession session = scope.DocumentSession;
                WaitForIndexing(scope.DocumentStore);
                DeviceFlowCode foundCode = await session
                    .Query<DeviceFlowCode>()
                    .SingleOrDefaultAsync(x => x.UserCode == testUserCode);
                foundCode.Should().NotBeNull();

                IMetadataDictionary documentMetadata = scope.DocumentSession.Advanced.GetMetadataFor(foundCode);
                documentMetadata.Should().ContainKey(Constants.Documents.Metadata.Expires);
                documentMetadata[Constants.Documents.Metadata.Expires]
                    .Should()
                    .Be(originalDeviceCode.CreationTime.AddSeconds(originalDeviceCode.Lifetime).ToUniversalTime()
                        .ToString("O"));

                // compare exchange expiry
                var result = await GetCompareExchangeAsync<string>(
                    scope.DocumentStore,
                    $"idsrv/devcode/{foundCode.DeviceCode}"
                );
                result.Should().NotBeNull();
                result.Metadata[Constants.Documents.Metadata.Expires]
                    .Should()
                    .Be(originalDeviceCode.CreationTime.AddSeconds(originalDeviceCode.Lifetime).ToUniversalTime()
                        .ToString("O"));
            }

            {
                var updatedDeviceCode = new DeviceCode
                {
                    ClientId = originalDeviceCode.ClientId,
                    RequestedScopes = originalDeviceCode.RequestedScopes,
                    CreationTime = DateTime.Now,
                    Lifetime = originalDeviceCode.Lifetime + 300,
                    IsOpenId = originalDeviceCode.IsOpenId,
                    Subject = originalDeviceCode.Subject,
                    Description = "Updated description",
                };

                await NewServiceScope(
                    options => { options.SetRavenDbDocumentExpiresMetadata = true; }
                ).DeviceFlowStore.UpdateByUserCodeAsync(testUserCode, updatedDeviceCode);

                {
                    ServiceScope scope = NewServiceScope();
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
                        .Be(originalDeviceCode.CreationTime.AddSeconds(originalDeviceCode.Lifetime).ToUniversalTime()
                            .ToString("O"));

                    // compare exchange expiry
                    var result = await GetCompareExchangeAsync<string>(
                        scope.DocumentStore,
                        $"idsrv/devcode/{foundCode.DeviceCode}"
                    );
                    result.Should().NotBeNull();
                    result.Metadata[Constants.Documents.Metadata.Expires]
                        .Should()
                        .Be(originalDeviceCode.CreationTime.AddSeconds(originalDeviceCode.Lifetime).ToUniversalTime()
                            .ToString("O"));
                }
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
                CreationTime = DateTime.Now,
                Lifetime = 300,
                IsOpenId = true,
                Subject = new ClaimsPrincipal(new ClaimsIdentity(new List<Claim>
                    { new Claim(JwtClaimTypes.Subject, $"sub_{Guid.NewGuid().ToString()}") }
                )),
            };

            await NewServiceScope(
                    options => { options.SetRavenDbDocumentExpiresMetadata = false; }
                )
                .DeviceFlowStore
                .StoreDeviceAuthorizationAsync(
                    testDeviceCode,
                    testUserCode,
                    deviceCode
                );

            {
                ServiceScope scope = NewServiceScope();
                IAsyncDocumentSession session = scope.DocumentSession;
                WaitForIndexing(scope.DocumentStore);
                DeviceFlowCode foundCode = await session
                    .Query<DeviceFlowCode>()
                    .SingleOrDefaultAsync(x => x.UserCode == testUserCode);
                foundCode.Should().NotBeNull();

                IMetadataDictionary metadata = scope.DocumentSession.Advanced.GetMetadataFor(foundCode);
                metadata.Should().NotContainKey(Constants.Documents.Metadata.Expires, "feature is disabled");

                // compare exchange expiry
                var result = await GetCompareExchangeAsync<string>(
                    scope.DocumentStore,
                    $"idsrv/devcode/{foundCode.DeviceCode}"
                );
                result.Should().NotBeNull();
                result.Metadata.Should().NotContainKey(Constants.Documents.Metadata.Expires);
            }

            deviceCode.Description = "Updated description";
            await NewServiceScope(
                options => { options.SetRavenDbDocumentExpiresMetadata = false; }
            ).DeviceFlowStore.UpdateByUserCodeAsync(testUserCode, deviceCode);

            {
                ServiceScope scope = NewServiceScope();
                IAsyncDocumentSession session = scope.DocumentSession;
                WaitForIndexing(scope.DocumentStore);
                DeviceFlowCode foundCode = await session
                    .Query<DeviceFlowCode>()
                    .SingleOrDefaultAsync(x => x.UserCode == testUserCode);
                foundCode.Should().NotBeNull();

                IMetadataDictionary metadata = scope.DocumentSession.Advanced.GetMetadataFor(foundCode);
                metadata.Should().NotContainKey(Constants.Documents.Metadata.Expires, "feature is disabled");

                // compare exchange expiry
                var result = await GetCompareExchangeAsync<string>(
                    scope.DocumentStore,
                    $"idsrv/devcode/{foundCode.DeviceCode}"
                );
                result.Should().NotBeNull();
                result.Metadata.Should().NotContainKey(Constants.Documents.Metadata.Expires);
            }
        }

        private ServiceScope NewServiceScope(Action<OperationalStoreOptions>? operationalStoreOptions = null)
            => InitializeServices(
                operationalStoreOptions,
                uniqueValuesReservationOptionsConfig: options => options.UseReservationDocumentsForUniqueValues = false
            );
    }
}