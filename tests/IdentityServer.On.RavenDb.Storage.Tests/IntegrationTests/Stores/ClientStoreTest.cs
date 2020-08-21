using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using IdentityModel;
using IdentityServer4.Models;
using IdentityServer4.Stores;
using Mcrio.IdentityServer.On.RavenDb.Storage.Stores.Additions;
using Xunit;
using Xunit.Sdk;
using ApiScope = Mcrio.IdentityServer.On.RavenDb.Storage.Entities.ApiScope;
using IdentityResource = Mcrio.IdentityServer.On.RavenDb.Storage.Entities.IdentityResource;

namespace Mcrio.IdentityServer.On.RavenDb.Storage.Tests.IntegrationTests.Stores
{
    public class ClientStoreTest : IntegrationTestBase
    {
        [Fact]
        public async Task FindClientByIdAsync_WhenClientExists_ExpectClientReturned()
        {
            var testClient = new Client
            {
                ClientId = "test_client",
                ClientName = "Test Client",
            };

            ServiceScope scope = InitializeServices();
            IClientStoreAdditions clientStoreAdditions = scope.ClientStoreAdditions;
            (await clientStoreAdditions.CreateAsync(testClient)).IsSuccess.Should().BeTrue();

            WaitForUserToContinueTheTest(scope.DocumentStore);

            Client client = await InitializeServices().ClientStore.FindClientByIdAsync(testClient.ClientId);

            client.Should().NotBeNull();
            client.Should().BeEquivalentTo(testClient);
            WaitForUserToContinueTheTest(scope.DocumentStore);
        }

        [Fact]
        public async Task FindClientByIdAsync_WhenClientDoesNotExist_ExpectNull()
        {
            Client? client = await InitializeServices().ClientStore.FindClientByIdAsync(Guid.NewGuid().ToString());
            client.Should().BeNull();
        }

        [Fact]
        public async Task FindClientByIdAsync_WhenClientExistsWithCollections_ExpectClientReturnedCollections()
        {
            var testClient = new Client
            {
                ClientId = "properties_test_client",
                ClientName = "Properties Test Client",
                AllowedCorsOrigins = { "https://localhost" },
                AllowedGrantTypes = new List<string>() { OidcConstants.GrantTypes.AuthorizationCode },
                AllowedScopes = { "openid", "profile", "api1" },
                Claims = { new ClientClaim("test", "value") },
                ClientSecrets = { new Secret("secret".Sha256()) },
                IdentityProviderRestrictions = { "AD" },
                PostLogoutRedirectUris = { "https://locahost/signout-callback" },
                Properties = { { "foo1", "bar1" }, { "foo2", "bar2" }, },
                RedirectUris = { "https://locahost/signin" },
            };

            ServiceScope scope1 = InitializeServices();
            (await scope1.ClientStoreAdditions.CreateAsync(testClient)).IsSuccess.Should().BeTrue();

            Client? client = await InitializeServices().ClientStore.FindClientByIdAsync(testClient.ClientId);

            client.Should().BeEquivalentTo(testClient);
        }

        [Fact]
        public async Task
            FindClientByIdAsync_WhenClientsExistWithManyCollections_ExpectClientReturnedInUnderFiveSeconds()
        {
            var testClient = new Client
            {
                ClientId = "test_client_with_uris",
                ClientName = "Test client with URIs",
                AllowedScopes = { "openid", "profile", "api1" },
                AllowedGrantTypes = new List<string>() { OidcConstants.GrantTypes.AuthorizationCode },
            };

            for (var i = 0; i < 50; i++)
            {
                testClient.RedirectUris.Add($"https://localhost/{i}");
                testClient.PostLogoutRedirectUris.Add($"https://localhost/{i}");
                testClient.AllowedCorsOrigins.Add($"https://localhost:{i}");
            }

            (await InitializeServices().ClientStoreAdditions.CreateAsync(testClient)).IsSuccess.Should().BeTrue();

            for (var i = 0; i < 200; i++)
            {
                ServiceScope scope34 = InitializeServices();
                await scope34.DocumentSession.StoreAsync(new IdentityResource()
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = "fdsfs",
                });
                await scope34.DocumentSession.StoreAsync(new ApiScope()
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = "fdss",
                });
                await scope34.DocumentSession.SaveChangesAsync();
                (await InitializeServices().ClientStoreAdditions.CreateAsync(
                    new Client
                    {
                        ClientId = testClient.ClientId + i,
                        ClientName = testClient.ClientName,
                        AllowedScopes = testClient.AllowedScopes,
                        AllowedGrantTypes = testClient.AllowedGrantTypes,
                        RedirectUris = testClient.RedirectUris,
                        PostLogoutRedirectUris = testClient.PostLogoutRedirectUris,
                        AllowedCorsOrigins = testClient.AllowedCorsOrigins,
                    }
                )).IsSuccess.Should().BeTrue();
            }

            ServiceScope scope = InitializeServices();
            WaitForUserToContinueTheTest(scope.DocumentStore);

            const int timeout = 5000;
            var task = Task.Run(() => InitializeServices().ClientStore.FindClientByIdAsync(testClient.ClientId));

            if (await Task.WhenAny(task, Task.Delay(timeout)) == task)
            {
                Client? client = task.Result;
                client.Should().BeEquivalentTo(testClient);
            }
            else
            {
                throw new TestTimeoutException(timeout);
            }
        }

        [Fact]
        public async Task ShouldAddClient()
        {
            var testClient = new Client
            {
                ClientId = "test_client_with_uris",
                ClientName = "Test client with URIs",
                AllowedScopes = { "openid", "profile", "api1" },
                AllowedGrantTypes = new List<string>() { OidcConstants.GrantTypes.AuthorizationCode },
            };

            (await InitializeServices().ClientStoreAdditions.CreateAsync(testClient)).IsSuccess.Should().BeTrue();
            Client? fromDb = await InitializeServices().ClientStore.FindClientByIdAsync(testClient.ClientId);
            fromDb.Should().NotBeNull();
            fromDb.Should().BeEquivalentTo(testClient);
        }

        [Fact]
        public async Task ShouldNotAddClientIfAlreadyExistsWithSameClientId()
        {
            Client testClient = new Client
            {
                ClientId = "test_client_with_uris",
                ClientName = "Test client with URIs",
                AllowedScopes = { "openid", "profile", "api1" },
                AllowedGrantTypes = new List<string>() { OidcConstants.GrantTypes.AuthorizationCode },
            };

            ServiceScope scope = InitializeServices();
            (await scope.ClientStoreAdditions.CreateAsync(testClient)).IsSuccess.Should().BeTrue();

            (await scope.ClientStoreAdditions.CreateAsync(testClient)).IsSuccess.Should().BeFalse("client already created.");

            var testClient2 = new Client
            {
                ClientId = "test_client_with_uris",
                ClientName = "Test client with URIs",
                AllowedScopes = { "openid", "profile", "api1" },
                AllowedGrantTypes = new List<string>() { OidcConstants.GrantTypes.AuthorizationCode },
            };
            (await scope.ClientStoreAdditions.CreateAsync(testClient2)).IsSuccess.Should()
                .BeFalse("client with same name already exists.");
        }

        [Fact]
        public async Task ShouldNotAddClientIfMissingClientId()
        {
            {
                StoreResult result = await InitializeServices().ClientStoreAdditions.CreateAsync(
                    new Client
                    {
                        ClientId = string.Empty,
                        ClientName = "Test client with URIs",
                    }
                );
                result.IsSuccess.Should().BeFalse();
                result.Error.Should().Be(ErrorDescriber.ClientIdMissing);
            }

            {
                StoreResult result = await InitializeServices().ClientStoreAdditions.CreateAsync(
                    new Client
                    {
                        ClientId = null,
                        ClientName = "Test client with URIs",
                    }
                );
                result.IsSuccess.Should().BeFalse();
                result.Error.Should().Be(ErrorDescriber.ClientIdMissing);
            }
        }

        [Fact]
        public async Task ShouldNotAddClientIfMissingProtocolType()
        {
            {
                StoreResult result = await InitializeServices().ClientStoreAdditions.CreateAsync(
                    new Client
                    {
                        ClientId = Guid.NewGuid().ToString(),
                        ClientName = "Test client with URIs",
                        ProtocolType = string.Empty,
                    }
                );
                result.IsSuccess.Should().BeFalse();
                result.Error.Should().Be(ErrorDescriber.ProtocolTypeMissing);
            }

            {
                StoreResult result = await InitializeServices().ClientStoreAdditions.CreateAsync(
                    new Client
                    {
                        ClientId = Guid.NewGuid().ToString(),
                        ClientName = "Test client with URIs",
                        ProtocolType = null,
                    }
                );
                result.IsSuccess.Should().BeFalse();
                result.Error.Should().Be(ErrorDescriber.ProtocolTypeMissing);
            }
        }

        [Fact]
        public async Task ShouldNotAllowExistingClientIdUpdate()
        {
            var testClient = new Client
            {
                ClientId = "test_client_with_uris",
                ClientName = "Test client with URIs",
                AllowedScopes = { "openid", "profile", "api1" },
                AllowedGrantTypes = new List<string>() { OidcConstants.GrantTypes.AuthorizationCode },
            };
            (await InitializeServices().ClientStoreAdditions.CreateAsync(testClient)).IsSuccess.Should().BeTrue();

            ServiceScope scope = InitializeServices();
            IClientStore clientStore = scope.ClientStore;

            Client? retrievedClient = await clientStore.FindClientByIdAsync(testClient.ClientId);
            retrievedClient.Should().NotBeNull();

            retrievedClient.ClientId = Guid.NewGuid().ToString();

            StoreResult updateResult = await scope.ClientStoreAdditions.UpdateAsync(retrievedClient);
            updateResult.IsSuccess.Should().BeFalse();
            updateResult.Error.Should().StartWith("Entity not found.");
        }

        [Fact]
        public async Task ShouldUpdateExistingClient()
        {
            var testClient = new Client
            {
                ClientId = "test_client_with_uris",
                ClientName = "Test client with URIs",
                AllowedScopes = { "openid", "profile", "api1" },
                AllowedGrantTypes = new List<string>() { OidcConstants.GrantTypes.AuthorizationCode },
            };
            (await InitializeServices().ClientStoreAdditions.CreateAsync(testClient)).IsSuccess.Should().BeTrue();

            var newName = Guid.NewGuid().ToString();
            var newScopes = new List<string> { "openid", "api1" };

            {
                ServiceScope scope = InitializeServices();
                IClientStore clientStore = scope.ClientStore;

                Client? retrievedClient = await clientStore.FindClientByIdAsync(testClient.ClientId);
                retrievedClient.Should().NotBeNull();

                retrievedClient.ClientName = newName;
                retrievedClient.AllowedScopes = newScopes;

                StoreResult updateResult = await scope.ClientStoreAdditions.UpdateAsync(retrievedClient);
                updateResult.IsSuccess.Should().BeTrue();
            }

            {
                ServiceScope scope = InitializeServices();
                IClientStore clientStore = scope.ClientStore;

                Client? retrievedClient = await clientStore.FindClientByIdAsync(testClient.ClientId);
                retrievedClient.Should().NotBeNull();

                retrievedClient.ClientName.Should().Be(newName);
                retrievedClient.AllowedScopes.Should().BeEquivalentTo(newScopes);

                retrievedClient.Should().NotBeEquivalentTo(testClient);
            }
        }

        [Fact]
        public async Task ShouldNotUpdateIfEntityUpdatedFromAnotherSession()
        {
            ServiceScope scope0 = InitializeServices();
            var testClient = new Client
            {
                ClientId = "test_client_with_uris",
                ClientName = "Test client with URIs",
                AllowedScopes = { "openid", "profile", "api1" },
                AllowedGrantTypes = new List<string>() { OidcConstants.GrantTypes.AuthorizationCode },
            };
            (await scope0.ClientStoreAdditions.CreateAsync(testClient)).IsSuccess.Should().BeTrue();

            ServiceScope scope1 = InitializeServices();
            ServiceScope scope2 = InitializeServices();

            var newName = Guid.NewGuid().ToString();
            var newApiScopes = new List<string> { "openid", "api1" };

            Client? retrievedClient1 = await scope1.ClientStore.FindClientByIdAsync(testClient.ClientId);
            retrievedClient1.Should().NotBeNull();

            {
                Client? retrievedClient2 = await scope2.ClientStore.FindClientByIdAsync(testClient.ClientId);
                retrievedClient2.Should().NotBeNull();

                retrievedClient2.ClientName = newName;
                retrievedClient2.AllowedScopes = newApiScopes;

                StoreResult updateResult2 = await scope2.ClientStoreAdditions.UpdateAsync(retrievedClient2);
                updateResult2.IsSuccess.Should().BeTrue();
            }

            retrievedClient1.ClientName = newName;
            retrievedClient1.AllowedScopes = newApiScopes;

            StoreResult updateResult = await scope1.ClientStoreAdditions.UpdateAsync(retrievedClient1);

            scope0.DocumentSession.Advanced.NumberOfRequests.Should().Be(1, "used to just insert test user");
            scope2.DocumentSession.Advanced.NumberOfRequests.Should().Be(2, "used to get user and update");
            scope1.DocumentSession.Advanced.NumberOfRequests.Should().Be(2, "used to get user and fail update");

            updateResult.IsSuccess.Should().BeFalse("client updated from another process.");
            updateResult.Error.Should().Be(ErrorDescriber.ConcurrencyException);
        }

        [Fact]
        public async Task ShouldDeleteExistingClient()
        {
            ServiceScope scope0 = InitializeServices();
            (await scope0.ClientStoreAdditions.CreateAsync(
                new Client
                {
                    ClientId = "test_client_with_uris",
                    ClientName = "Test client with URIs",
                    AllowedScopes = { "openid", "profile", "api1" },
                    AllowedGrantTypes = new List<string>() { OidcConstants.GrantTypes.AuthorizationCode },
                }
            )).IsSuccess.Should().BeTrue();
            (await scope0.ClientStoreAdditions.CreateAsync(
                new Client
                {
                    ClientId = "test_client_with_uris_2",
                    ClientName = "Test client with URIs",
                    AllowedScopes = { "openid", "profile", "api1" },
                    AllowedGrantTypes = new List<string>() { OidcConstants.GrantTypes.AuthorizationCode },
                }
            )).IsSuccess.Should().BeTrue();

            {
                ServiceScope scope2 = InitializeServices();
                Client? retrievedClient1 = await scope2.ClientStore.FindClientByIdAsync("test_client_with_uris");
                retrievedClient1.Should().NotBeNull();

                Client? retrievedClient2 = await scope2.ClientStore.FindClientByIdAsync("test_client_with_uris_2");
                retrievedClient2.Should().NotBeNull();

                (await scope2.ClientStoreAdditions.DeleteAsync("test_client_with_uris")).IsSuccess.Should().BeTrue();
            }

            {
                ServiceScope scope2 = InitializeServices();
                Client? retrievedClient1 = await scope2.ClientStore.FindClientByIdAsync("test_client_with_uris");
                retrievedClient1.Should().BeNull("we deleted this client");

                Client? retrievedClient2 = await scope2.ClientStore.FindClientByIdAsync("test_client_with_uris_2");
                retrievedClient2.Should().NotBeNull("this client was not deleted");
            }

            StoreResult removeNonExistingResult = await InitializeServices()
                .ClientStoreAdditions
                .DeleteAsync("test_client_with_uris");
            removeNonExistingResult.IsSuccess.Should().BeFalse();
            removeNonExistingResult.Error.Should().StartWith("Entity not found");
        }
    }
}