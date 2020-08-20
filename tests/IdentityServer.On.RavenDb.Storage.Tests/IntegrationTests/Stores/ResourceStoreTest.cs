using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using IdentityModel;
using IdentityServer4.Models;
using IdentityServer4.Stores;
using Mcrio.IdentityServer.On.RavenDb.Storage.Stores.Advanced;
using Xunit;

namespace Mcrio.IdentityServer.On.RavenDb.Storage.Tests.IntegrationTests.Stores
{
    public class ResourceStoreTest : IntegrationTestBase
    {
        [Fact]
        public async Task FindApiResourcesByNameAsync_WhenResourceExists_ExpectResourceAndCollectionsReturned()
        {
            ApiResource resource = CreateApiResourceTestResource();

            ServiceScope scope = InitializeServices();
            (await scope.ResourceStoreAdditions.CreateApiResourceAsync(resource)).IsSuccess.Should().BeTrue();

            ApiResource? foundResource = (
                await InitializeServices().ResourceStore.FindApiResourcesByNameAsync(new[] { resource.Name })
            ).SingleOrDefault();

            Assert.NotNull(foundResource);
            Assert.True(foundResource!.Name == resource.Name);

            Assert.NotNull(foundResource.UserClaims);
            Assert.NotEmpty(foundResource.UserClaims);
            Assert.NotNull(foundResource.ApiSecrets);
            Assert.NotEmpty(foundResource.ApiSecrets);
            Assert.NotNull(foundResource.Scopes);
            Assert.NotEmpty(foundResource.Scopes);

            foundResource.Should().BeEquivalentTo(resource);
        }

        [Fact]
        public async Task FindApiResourcesByNameAsync_WhenResourcesExist_ExpectOnlyResourcesRequestedReturned()
        {
            ApiResource resource = CreateApiResourceTestResource();

            ServiceScope scope = InitializeServices();
            (await scope.ResourceStoreAdditions.CreateApiResourceAsync(resource)).IsSuccess.Should().BeTrue();
            (await scope.ResourceStoreAdditions.CreateApiResourceAsync(CreateApiResourceTestResource()))
                .IsSuccess
                .Should()
                .BeTrue();

            ApiResource? foundResource = (
                await InitializeServices().ResourceStore.FindApiResourcesByNameAsync(new[] { resource.Name })
            ).SingleOrDefault();

            Assert.NotNull(foundResource);
            Assert.True(foundResource!.Name == resource.Name);

            Assert.NotNull(foundResource.UserClaims);
            Assert.NotEmpty(foundResource.UserClaims);
            Assert.NotNull(foundResource.ApiSecrets);
            Assert.NotEmpty(foundResource.ApiSecrets);
            Assert.NotNull(foundResource.Scopes);
            Assert.NotEmpty(foundResource.Scopes);
        }

        [Fact]
        public async Task FindApiResourcesByScopeNameAsync_WhenResourcesExist_ExpectResourcesReturned()
        {
            ApiResource testApiResource = CreateApiResourceTestResource();
            ApiScope testApiScope = CreateApiScopeTestResource();
            testApiResource.Scopes.Add(testApiScope.Name);

            ServiceScope scope = InitializeServices();
            (await scope.ResourceStoreAdditions.CreateApiResourceAsync(testApiResource)).IsSuccess.Should().BeTrue();
            (await scope.ResourceStoreAdditions.CreateApiScopeAsync(testApiScope)).IsSuccess.Should().BeTrue();

            List<ApiResource> resources = (
                await InitializeServices()
                    .ResourceStore
                    .FindApiResourcesByScopeNameAsync(new List<string>
                    {
                        testApiScope.Name,
                    })
            ).ToList();

            Assert.NotNull(resources);
            Assert.NotEmpty(resources);
            Assert.NotNull(resources.Single(x => x.Name == testApiResource.Name));
        }

        [Fact]
        public async Task FindApiResourcesByScopeNameAsync_WhenResourcesExist_ExpectOnlyResourcesRequestedReturned()
        {
            IdentityResource testIdentityResource = CreateIdentityTestResource();
            ApiResource testApiResource = CreateApiResourceTestResource();
            ApiScope testApiScope = CreateApiScopeTestResource();
            testApiResource.Scopes.Add(testApiScope.Name);

            ServiceScope scope = InitializeServices();
            (await scope.ResourceStoreAdditions.CreateIdentityResourceAsync(testIdentityResource)).IsSuccess.Should()
                .BeTrue();
            (await scope.ResourceStoreAdditions.CreateApiResourceAsync(testApiResource)).IsSuccess.Should().BeTrue();
            (await scope.ResourceStoreAdditions.CreateApiScopeAsync(testApiScope)).IsSuccess.Should().BeTrue();
            (await scope.ResourceStoreAdditions.CreateIdentityResourceAsync(CreateIdentityTestResource())).IsSuccess
                .Should()
                .BeTrue();
            (await scope.ResourceStoreAdditions.CreateApiResourceAsync(CreateApiResourceTestResource())).IsSuccess
                .Should()
                .BeTrue();
            (await scope.ResourceStoreAdditions.CreateApiScopeAsync(CreateApiScopeTestResource())).IsSuccess.Should()
                .BeTrue();

            WaitForIndexing(scope.DocumentStore);

            List<ApiResource> resources = (
                await InitializeServices()
                    .ResourceStore
                    .FindApiResourcesByScopeNameAsync(new[] { testApiScope.Name })
            ).ToList();

            Assert.NotNull(resources);
            Assert.NotEmpty(resources);
            Assert.NotNull(resources.Single(x => x.Name == testApiResource.Name));
        }

        [Fact]
        public async Task
            FindIdentityResourcesByScopeNameAsync_WhenResourceExists_ExpectResourceAndCollectionsReturned()
        {
            IdentityResource resource = CreateIdentityTestResource();

            ServiceScope scope = InitializeServices();
            (await scope.ResourceStoreAdditions.CreateIdentityResourceAsync(resource)).IsSuccess.Should().BeTrue();

            WaitForIndexing(scope.DocumentStore);

            IList<IdentityResource> resources = (
                await InitializeServices()
                    .ResourceStore
                    .FindIdentityResourcesByScopeNameAsync(new List<string>
                    {
                        resource.Name,
                    })
            ).ToList();

            WaitForUserToContinueTheTest(scope.DocumentStore);

            Assert.NotNull(resources);
            Assert.NotEmpty(resources);
            IdentityResource? foundScope = resources.Single();

            Assert.Equal(resource.Name, foundScope.Name);
            Assert.NotNull(foundScope.UserClaims);
            Assert.NotEmpty(foundScope.UserClaims);
        }

        [Fact]
        public async Task FindIdentityResourcesByScopeNameAsync_WhenResourcesExist_ExpectOnlyRequestedReturned()
        {
            IdentityResource resource = CreateIdentityTestResource();

            ServiceScope scope = InitializeServices();
            (await scope.ResourceStoreAdditions.CreateIdentityResourceAsync(resource)).IsSuccess.Should().BeTrue();
            (await scope.ResourceStoreAdditions.CreateIdentityResourceAsync(CreateIdentityTestResource()))
                .IsSuccess
                .Should()
                .BeTrue();

            IList<IdentityResource> resources = (
                await InitializeServices()
                    .ResourceStore
                    .FindIdentityResourcesByScopeNameAsync(new List<string>
                    {
                        resource.Name,
                    })
            ).ToList();

            Assert.NotNull(resources);
            Assert.NotEmpty(resources);
            Assert.NotNull(resources.Single(x => x.Name == resource.Name));
        }

        [Fact]
        public async Task FindApiScopesByNameAsync_WhenResourceExists_ExpectResourceAndCollectionsReturned()
        {
            ApiScope resource = CreateApiScopeTestResource();

            ServiceScope scope = InitializeServices();
            (await scope.ResourceStoreAdditions.CreateApiScopeAsync(resource)).IsSuccess.Should().BeTrue();

            IList<ApiScope> resources = (
                await InitializeServices()
                    .ResourceStore
                    .FindApiScopesByNameAsync(new List<string>
                    {
                        resource.Name,
                    })
            ).ToList();

            Assert.NotNull(resources);
            Assert.NotEmpty(resources);
            ApiScope foundScope = resources.Single();

            Assert.Equal(resource.Name, foundScope.Name);
            Assert.NotNull(foundScope.UserClaims);
            Assert.NotEmpty(foundScope.UserClaims);
        }

        [Fact]
        public async Task FindApiScopesByNameAsync_WhenResourcesExist_ExpectOnlyRequestedReturned()
        {
            ApiScope resource = CreateApiScopeTestResource();

            ServiceScope scope = InitializeServices();
            (await scope.ResourceStoreAdditions.CreateApiScopeAsync(resource)).IsSuccess.Should().BeTrue();
            (await scope.ResourceStoreAdditions.CreateApiScopeAsync(CreateApiScopeTestResource())).IsSuccess.Should()
                .BeTrue();

            IList<ApiScope> resources = (
                await InitializeServices()
                    .ResourceStore
                    .FindApiScopesByNameAsync(new List<string>
                    {
                        resource.Name,
                    })
            ).ToList();

            Assert.NotNull(resources);
            Assert.NotEmpty(resources);
            Assert.NotNull(resources.Single(x => x.Name == resource.Name));
        }

        [Fact]
        public async Task GetAllResources_WhenAllResourcesRequested_ExpectAllResourcesIncludingHidden()
        {
            IdentityResource visibleIdentityResource = CreateIdentityTestResource();
            ApiResource visibleApiResource = CreateApiResourceTestResource();
            ApiScope visibleApiScope = CreateApiScopeTestResource();
            var hiddenIdentityResource = new IdentityResource
            {
                Name = Guid.NewGuid().ToString(),
                ShowInDiscoveryDocument = false,
            };
            var hiddenApiResource = new ApiResource
            {
                Name = Guid.NewGuid().ToString(),
                Scopes = { Guid.NewGuid().ToString() },
                ShowInDiscoveryDocument = false,
            };
            var hiddenApiScope = new ApiScope
            {
                Name = Guid.NewGuid().ToString(),
                ShowInDiscoveryDocument = false,
            };

            ServiceScope scope = InitializeServices();

            (await scope.ResourceStoreAdditions.CreateIdentityResourceAsync(visibleIdentityResource))
                .IsSuccess
                .Should()
                .BeTrue();
            (await scope.ResourceStoreAdditions.CreateApiResourceAsync(visibleApiResource)).IsSuccess.Should().BeTrue();
            (await scope.ResourceStoreAdditions.CreateApiScopeAsync(visibleApiScope)).IsSuccess.Should().BeTrue();

            (await scope.ResourceStoreAdditions.CreateIdentityResourceAsync(hiddenIdentityResource)).IsSuccess.Should()
                .BeTrue();
            (await scope.ResourceStoreAdditions.CreateApiResourceAsync(hiddenApiResource)).IsSuccess.Should().BeTrue();
            (await scope.ResourceStoreAdditions.CreateApiScopeAsync(hiddenApiScope)).IsSuccess.Should().BeTrue();

            Resources resources = await InitializeServices().ResourceStore.GetAllResourcesAsync();

            Assert.NotNull(resources);
            Assert.NotEmpty(resources.IdentityResources);
            Assert.NotEmpty(resources.ApiResources);
            Assert.NotEmpty(resources.ApiScopes);

            Assert.Contains(resources.IdentityResources, x => x.Name == visibleIdentityResource.Name);
            Assert.Contains(resources.IdentityResources, x => x.Name == hiddenIdentityResource.Name);

            Assert.Contains(resources.ApiResources, x => x.Name == visibleApiResource.Name);
            Assert.Contains(resources.ApiResources, x => x.Name == hiddenApiResource.Name);

            Assert.Contains(resources.ApiScopes, x => x.Name == visibleApiScope.Name);
            Assert.Contains(resources.ApiScopes, x => x.Name == hiddenApiScope.Name);
        }

        #region IdentityResource crud region

        [Fact]
        public async Task ShouldAddIdentityResource()
        {
            var testIdentityResource = new IdentityResource()
            {
                Name = "test-name",
            };

            (await InitializeServices().ResourceStoreAdditions.CreateIdentityResourceAsync(testIdentityResource))
                .IsSuccess
                .Should().BeTrue();
            var fromDb = (await InitializeServices()
                .ResourceStore
                .FindIdentityResourcesByScopeNameAsync(new[] { testIdentityResource.Name })).ToList();
            fromDb.Should().NotBeNull();
            fromDb.Should().ContainSingle();
            fromDb.Should().BeEquivalentTo(testIdentityResource);
        }

        [Fact]
        public async Task ShouldNotAddIdentityResourceIfAlreadyExistsWithSameName()
        {
            var testIdentityResource = new IdentityResource
            {
                Name = "test-name",
            };

            (await InitializeServices().ResourceStoreAdditions.CreateIdentityResourceAsync(testIdentityResource))
                .IsSuccess
                .Should()
                .BeTrue();
            (await InitializeServices().ResourceStoreAdditions.CreateIdentityResourceAsync(testIdentityResource))
                .IsSuccess
                .Should()
                .BeFalse("client already created.");

            var testIdentityResource2 = new IdentityResource
            {
                Name = "test-name",
            };
            (await InitializeServices().ResourceStoreAdditions.CreateIdentityResourceAsync(testIdentityResource2))
                .IsSuccess
                .Should()
                .BeFalse("client with same name already exists.");
        }

        [Fact]
        public async Task ShouldNotAddIdentityResourceIfMissingName()
        {
            {
                StoreResult result = await InitializeServices().ResourceStoreAdditions.CreateIdentityResourceAsync(
                    new IdentityResource
                    {
                        Name = string.Empty,
                    }
                );
                result.IsSuccess.Should().BeFalse();
                result.Error.Should().Be(ErrorDescriber.IdentityResourceNameMissing);
            }

            {
                StoreResult result = await InitializeServices().ResourceStoreAdditions.CreateIdentityResourceAsync(
                    new IdentityResource
                    {
                        Name = null,
                    }
                );
                result.IsSuccess.Should().BeFalse();
                result.Error.Should().Be(ErrorDescriber.IdentityResourceNameMissing);
            }
        }

        [Fact]
        public async Task ShouldNotAllowExistingIdentityResourceNameUpdate()
        {
            var testIdentityResource = new IdentityResource
            {
                Name = "test-name",
            };

            (await InitializeServices().ResourceStoreAdditions.CreateIdentityResourceAsync(testIdentityResource))
                .IsSuccess
                .Should()
                .BeTrue();

            ServiceScope scope = InitializeServices();
            IResourceStore resourceStore = scope.ResourceStore;

            var dbResults = (await resourceStore
                .FindIdentityResourcesByScopeNameAsync(new[] { testIdentityResource.Name })).ToList();
            dbResults.Should().NotBeNull();
            dbResults.Should().ContainSingle();

            IdentityResource retrievedResource = dbResults.First();
            retrievedResource.Name = Guid.NewGuid().ToString();

            StoreResult updateResult =
                await scope.ResourceStoreAdditions.UpdateIdentityResourceAsync(retrievedResource);
            updateResult.IsSuccess.Should().BeFalse();
            updateResult.Error.Should().StartWith("Entity not found.");
        }

        [Fact]
        public async Task ShouldUpdateExistingIdentityResource()
        {
            var testIdentityResource = new IdentityResource
            {
                Name = "test-name",
                DisplayName = "TestName",
                UserClaims = new List<string> { "claim1", "claim2" },
            };

            (await InitializeServices().ResourceStoreAdditions.CreateIdentityResourceAsync(testIdentityResource))
                .IsSuccess
                .Should()
                .BeTrue();

            var newDisplayName = Guid.NewGuid().ToString();
            var newClaims = new List<string> { "claim4", "claim5" };

            WaitForUserToContinueTheTest(InitializeServices().DocumentStore);

            {
                ServiceScope scope = InitializeServices();
                IResourceStore resourceStore = scope.ResourceStore;

                var dbResults = (await resourceStore
                    .FindIdentityResourcesByScopeNameAsync(new[] { testIdentityResource.Name })).ToList();
                dbResults.Should().NotBeNull();
                dbResults.Should().ContainSingle();

                IdentityResource retrievedResource = dbResults.First();
                retrievedResource.DisplayName = newDisplayName;
                retrievedResource.UserClaims = newClaims;

                StoreResult updateResult =
                    await scope.ResourceStoreAdditions.UpdateIdentityResourceAsync(retrievedResource);
                updateResult.IsSuccess.Should().BeTrue();
            }

            {
                ServiceScope scope = InitializeServices();
                IResourceStore resourceStore = scope.ResourceStore;

                var dbResults = (await resourceStore
                    .FindIdentityResourcesByScopeNameAsync(new[] { testIdentityResource.Name })).ToList();
                dbResults.Should().NotBeNull();
                dbResults.Should().ContainSingle();

                IdentityResource retrievedResource = dbResults.First();
                retrievedResource.DisplayName.Should().Be(newDisplayName);
                retrievedResource.UserClaims.Should().BeEquivalentTo(newClaims);
                retrievedResource.Should().NotBeEquivalentTo(testIdentityResource);
            }
        }

        [Fact]
        public async Task ShouldNotUpdateIdentityResourceIfEntityUpdatedFromAnotherSession()
        {
            var testIdentityResource = new IdentityResource
            {
                Name = "test-name",
                DisplayName = "testName",
                UserClaims = new List<string> { "claim1", "claim2" },
            };

            ServiceScope scope0 = InitializeServices();
            (await scope0.ResourceStoreAdditions.CreateIdentityResourceAsync(testIdentityResource))
                .IsSuccess
                .Should()
                .BeTrue();

            ServiceScope scope1 = InitializeServices();
            ServiceScope scope2 = InitializeServices();

            var newDisplayName = Guid.NewGuid().ToString();
            var newClaims = new List<string> { "claim4", "claim5" };

            var dbResults = (await scope1.ResourceStore
                .FindIdentityResourcesByScopeNameAsync(new[] { testIdentityResource.Name })).ToList();
            dbResults.Should().NotBeNull();
            dbResults.Should().ContainSingle();

            IdentityResource retrievedResource = dbResults.First();
            retrievedResource.Should().NotBeNull();

            {
                var dbResults2 = (
                    await scope2.ResourceStore
                        .FindIdentityResourcesByScopeNameAsync(new[] { testIdentityResource.Name })
                ).ToList();
                dbResults2.Should().NotBeNull();
                dbResults2.Should().ContainSingle();

                IdentityResource retrievedResource2 = dbResults2.First();
                retrievedResource2.Should().NotBeNull();

                retrievedResource2.DisplayName = newDisplayName;
                retrievedResource2.UserClaims = newClaims;

                StoreResult updateResult2 =
                    await scope2.ResourceStoreAdditions.UpdateIdentityResourceAsync(retrievedResource2);
                updateResult2.IsSuccess.Should().BeTrue();
            }

            retrievedResource.DisplayName = newDisplayName;
            retrievedResource.UserClaims = newClaims;

            StoreResult updateResult =
                await scope1.ResourceStoreAdditions.UpdateIdentityResourceAsync(retrievedResource);

            scope0.DocumentSession.Advanced.NumberOfRequests.Should().Be(1, "used to just insert");
            scope2.DocumentSession.Advanced.NumberOfRequests.Should().Be(2, "used to get and update");
            scope1.DocumentSession.Advanced.NumberOfRequests.Should().Be(2, "used to get and fail update");

            updateResult.IsSuccess.Should().BeFalse("resource updated from another process.");
            updateResult.Error.Should().Be(ErrorDescriber.ConcurrencyException);
        }

        [Fact]
        public async Task ShouldDeleteExistingIdentityResource()
        {
            ServiceScope scope0 = InitializeServices();
            (await scope0.ResourceStoreAdditions.CreateIdentityResourceAsync(
                new IdentityResource
                {
                    Name = "test-name",
                    UserClaims = new List<string> { "claim1", "claim2" },
                }
            )).IsSuccess.Should().BeTrue();
            (await scope0.ResourceStoreAdditions.CreateIdentityResourceAsync(
                new IdentityResource
                {
                    Name = "test-name-2",
                    UserClaims = new List<string> { "claim1", "claim4" },
                }
            )).IsSuccess.Should().BeTrue();

            {
                ServiceScope scope2 = InitializeServices();
                var dbResults = (await scope2.ResourceStore
                    .FindIdentityResourcesByScopeNameAsync(new[] { "test-name", "test-name-2" })).ToList();

                dbResults.Should().NotBeNull();
                dbResults.Should().HaveCount(2);

                IdentityResource retrievedResource = dbResults.First();
                retrievedResource.Should().NotBeNull();

                IdentityResource retrievedResource2 = dbResults.Last();
                retrievedResource2.Should().NotBeNull();

                (await scope2.ResourceStoreAdditions.DeleteIdentityResourceAsync("test-name-2")).IsSuccess.Should().BeTrue();
            }

            {
                ServiceScope scope2 = InitializeServices();
                var dbResults = (await scope2.ResourceStore
                    .FindIdentityResourcesByScopeNameAsync(new[] { "test-name", "test-name-2" })).ToList();

                dbResults.Should().NotBeNull();
                dbResults.Should().ContainSingle();
                
                IdentityResource retrievedResource = dbResults.First();
                retrievedResource.Name.Should().Be("test-name");
            }

            StoreResult removeNonExistingResult = await InitializeServices()
                .ResourceStoreAdditions
                .DeleteIdentityResourceAsync("non-existing-name");
            removeNonExistingResult.IsSuccess.Should().BeFalse();
            removeNonExistingResult.Error.Should().StartWith("Entity not found");
        }

        #endregion

        #region ApiResource crud region

        [Fact]
        public async Task ShouldAddApiResource()
        {
            var testResource = new ApiResource()
            {
                Name = "test-name",
            };

            (await InitializeServices().ResourceStoreAdditions.CreateApiResourceAsync(testResource)).IsSuccess
                .Should().BeTrue();
            var fromDb = (await InitializeServices()
                .ResourceStore
                .FindApiResourcesByNameAsync(new[] { testResource.Name })).ToList();
            fromDb.Should().NotBeNull();
            fromDb.Should().ContainSingle();
            fromDb.Should().BeEquivalentTo(testResource);
        }

        [Fact]
        public async Task ShouldNotAddApiResourceIfAlreadyExistsWithSameName()
        {
            var testResource = new ApiResource()
            {
                Name = "test-name",
            };

            (await InitializeServices().ResourceStoreAdditions.CreateApiResourceAsync(testResource))
                .IsSuccess
                .Should()
                .BeTrue();
            (await InitializeServices().ResourceStoreAdditions.CreateApiResourceAsync(testResource))
                .IsSuccess
                .Should()
                .BeFalse("resource already created.");

            var restResource2 = new ApiResource()
            {
                Name = "test-name",
            };
            (await InitializeServices().ResourceStoreAdditions.CreateApiResourceAsync(restResource2))
                .IsSuccess
                .Should()
                .BeFalse("resource with same name already exists.");
        }

        [Fact]
        public async Task ShouldNotAddApiResourceIfMissingName()
        {
            {
                var result = await InitializeServices().ResourceStoreAdditions.CreateApiResourceAsync(
                    new ApiResource
                    {
                        Name = string.Empty,
                    }
                );
                result.IsSuccess.Should().BeFalse();
                result.Error.Should().Be(ErrorDescriber.ApiResourceNameMissing);
            }

            {
                var result = await InitializeServices().ResourceStoreAdditions.CreateApiResourceAsync(
                    new ApiResource
                    {
                        Name = null,
                    }
                );
                result.IsSuccess.Should().BeFalse();
                result.Error.Should().Be(ErrorDescriber.ApiResourceNameMissing);
            }
        }

        [Fact]
        public async Task ShouldNotAllowExistingApiResourceNameUpdate()
        {
            var testResource = new ApiResource
            {
                Name = "test-name",
            };

            (await InitializeServices().ResourceStoreAdditions.CreateApiResourceAsync(testResource))
                .IsSuccess
                .Should()
                .BeTrue();

            var scope = InitializeServices();
            var resourceStore = scope.ResourceStore;

            var dbResults = (await resourceStore
                .FindApiResourcesByNameAsync(new[] { testResource.Name })).ToList();
            dbResults.Should().NotBeNull();
            dbResults.Should().ContainSingle();

            var retrievedResource = dbResults.First();
            retrievedResource.Name = Guid.NewGuid().ToString();

            var updateResult = await scope.ResourceStoreAdditions.UpdateApiResourceAsync(retrievedResource);
            updateResult.IsSuccess.Should().BeFalse();
            updateResult.Error.Should().StartWith("Entity not found.");
        }

        [Fact]
        public async Task ShouldUpdateExistingApiResource()
        {
            var testResource = new ApiResource
            {
                Name = "test-name",
                DisplayName = "TestName",
                UserClaims = new List<string> { "claim1", "claim2" },
            };

            (await InitializeServices().ResourceStoreAdditions.CreateApiResourceAsync(testResource))
                .IsSuccess
                .Should()
                .BeTrue();

            var newDisplayName = Guid.NewGuid().ToString();
            var newClaims = new List<string> { "claim4", "claim5" };

            WaitForUserToContinueTheTest(InitializeServices().DocumentStore);

            {
                var scope = InitializeServices();
                var resourceStore = scope.ResourceStore;

                var dbResults = (await resourceStore
                    .FindApiResourcesByNameAsync(new[] { testResource.Name })).ToList();
                dbResults.Should().NotBeNull();
                dbResults.Should().ContainSingle();

                var retrievedResource = dbResults.First();
                retrievedResource.DisplayName = newDisplayName;
                retrievedResource.UserClaims = newClaims;

                var updateResult = await scope.ResourceStoreAdditions.UpdateApiResourceAsync(retrievedResource);
                updateResult.IsSuccess.Should().BeTrue();
            }

            {
                var scope = InitializeServices();
                var resourceStore = scope.ResourceStore;

                var dbResults = (await resourceStore
                    .FindApiResourcesByNameAsync(new[] { testResource.Name })).ToList();
                dbResults.Should().NotBeNull();
                dbResults.Should().ContainSingle();

                var retrievedResource = dbResults.First();
                retrievedResource.DisplayName.Should().Be(newDisplayName);
                retrievedResource.UserClaims.Should().BeEquivalentTo(newClaims);
                retrievedResource.Should().NotBeEquivalentTo(testResource);
            }
        }

        [Fact]
        public async Task ShouldNotUpdateApiResourceIfEntityUpdatedFromAnotherSession()
        {
            var testResource = new ApiResource
            {
                Name = "test-name",
                DisplayName = "testName",
                UserClaims = new List<string> { "claim1", "claim2" },
            };

            var scope0 = InitializeServices();
            (await scope0.ResourceStoreAdditions.CreateApiResourceAsync(testResource))
                .IsSuccess
                .Should()
                .BeTrue();

            var scope1 = InitializeServices();
            var scope2 = InitializeServices();

            var newDisplayName = Guid.NewGuid().ToString();
            var newClaims = new List<string> { "claim4", "claim5" };

            var dbResults = (await scope1.ResourceStore
                .FindApiResourcesByNameAsync(new[] { testResource.Name })).ToList();
            dbResults.Should().NotBeNull();
            dbResults.Should().ContainSingle();

            var retrievedResource = dbResults.First();
            retrievedResource.Should().NotBeNull();

            {
                var dbResults2 = (await scope2.ResourceStore
                    .FindApiResourcesByNameAsync(new[] { testResource.Name })).ToList();
                dbResults2.Should().NotBeNull();
                dbResults2.Should().ContainSingle();

                var retrievedResource2 = dbResults2.First();
                retrievedResource2.Should().NotBeNull();

                retrievedResource2.DisplayName = newDisplayName;
                retrievedResource2.UserClaims = newClaims;

                var updateResult2 = await scope2.ResourceStoreAdditions.UpdateApiResourceAsync(retrievedResource2);
                updateResult2.IsSuccess.Should().BeTrue();
            }

            retrievedResource.DisplayName = newDisplayName;
            retrievedResource.UserClaims = newClaims;

            var updateResult = await scope1.ResourceStoreAdditions.UpdateApiResourceAsync(retrievedResource);

            scope0.DocumentSession.Advanced.NumberOfRequests.Should().Be(1, "used to just insert");
            scope2.DocumentSession.Advanced.NumberOfRequests.Should().Be(2, "used to get and update");
            scope1.DocumentSession.Advanced.NumberOfRequests.Should().Be(2, "used to get and fail update");

            updateResult.IsSuccess.Should().BeFalse("resource updated from another process.");
            updateResult.Error.Should().Be(ErrorDescriber.ConcurrencyException);
        }

        [Fact]
        public async Task ShouldDeleteExistingApiResource()
        {
            var scope0 = InitializeServices();
            (await scope0.ResourceStoreAdditions.CreateApiResourceAsync(
                new ApiResource
                {
                    Name = "test-name",
                    UserClaims = new List<string> { "claim1", "claim2" },
                }
            )).IsSuccess.Should().BeTrue();
            (await scope0.ResourceStoreAdditions.CreateApiResourceAsync(
                new ApiResource
                {
                    Name = "test-name-2",
                    UserClaims = new List<string> { "claim1", "claim4" },
                }
            )).IsSuccess.Should().BeTrue();

            {
                var scope2 = InitializeServices();
                var dbResults = (await scope2.ResourceStore
                    .FindApiResourcesByNameAsync(new[] { "test-name", "test-name-2" })).ToList();

                dbResults.Should().NotBeNull();
                dbResults.Should().HaveCount(2);

                var retrievedResource = dbResults.First();
                retrievedResource.Should().NotBeNull();

                var retrievedResource2 = dbResults.Last();
                retrievedResource2.Should().NotBeNull();

                (await scope2.ResourceStoreAdditions.DeleteApiResourceAsync("test-name-2")).IsSuccess.Should().BeTrue();
            }

            {
                var scope2 = InitializeServices();
                var dbResults = (await scope2.ResourceStore
                    .FindApiResourcesByNameAsync(new[] { "test-name", "test-name-2" })).ToList();

                dbResults.Should().NotBeNull();
                dbResults.Should().ContainSingle();

                var retrievedResource = dbResults.First();
                retrievedResource.Name.Should().Be("test-name");
            }

            var removeNonExistingResult = await InitializeServices()
                .ResourceStoreAdditions
                .DeleteApiResourceAsync("non-existing-name");
            removeNonExistingResult.IsSuccess.Should().BeFalse();
            removeNonExistingResult.Error.Should().StartWith("Entity not found");
        }

        #endregion

        #region ApiScope crud region

        [Fact]
        public async Task ShouldAddApiScope()
        {
            var testResource = new ApiScope
            {
                Name = "test-name",
            };

            (await InitializeServices().ResourceStoreAdditions.CreateApiScopeAsync(testResource)).IsSuccess
                .Should().BeTrue();
            var fromDb = (await InitializeServices()
                .ResourceStore
                .FindApiScopesByNameAsync(new[] { testResource.Name })).ToList();
            fromDb.Should().NotBeNull();
            fromDb.Should().ContainSingle();
            fromDb.Should().BeEquivalentTo(testResource);
        }

        [Fact]
        public async Task ShouldNotAddApiScopeIfAlreadyExistsWithSameName()
        {
            var testResource = new ApiScope
            {
                Name = "test-name",
            };

            (await InitializeServices().ResourceStoreAdditions.CreateApiScopeAsync(testResource))
                .IsSuccess
                .Should()
                .BeTrue();
            (await InitializeServices().ResourceStoreAdditions.CreateApiScopeAsync(testResource))
                .IsSuccess
                .Should()
                .BeFalse("resource already created.");

            var restResource2 = new ApiScope
            {
                Name = "test-name",
            };
            (await InitializeServices().ResourceStoreAdditions.CreateApiScopeAsync(restResource2))
                .IsSuccess
                .Should()
                .BeFalse("resource with same name already exists.");
        }

        [Fact]
        public async Task ShouldNotAddApiScopeIfMissingName()
        {
            {
                var result = await InitializeServices().ResourceStoreAdditions.CreateApiScopeAsync(
                    new ApiScope
                    {
                        Name = string.Empty,
                    }
                );
                result.IsSuccess.Should().BeFalse();
                result.Error.Should().Be(ErrorDescriber.ApiScopeNameMissing);
            }

            {
                var result = await InitializeServices().ResourceStoreAdditions.CreateApiScopeAsync(
                    new ApiScope
                    {
                        Name = null,
                    }
                );
                result.IsSuccess.Should().BeFalse();
                result.Error.Should().Be(ErrorDescriber.ApiScopeNameMissing);
            }
        }

        [Fact]
        public async Task ShouldNotAllowExistingApiScopeNameUpdate()
        {
            var testResource = new ApiScope
            {
                Name = "test-name",
            };

            (await InitializeServices().ResourceStoreAdditions.CreateApiScopeAsync(testResource))
                .IsSuccess
                .Should()
                .BeTrue();

            var scope = InitializeServices();
            var resourceStore = scope.ResourceStore;

            var dbResults = (await resourceStore
                .FindApiScopesByNameAsync(new[] { testResource.Name })).ToList();
            dbResults.Should().NotBeNull();
            dbResults.Should().ContainSingle();

            var retrievedResource = dbResults.First();
            retrievedResource.Name = Guid.NewGuid().ToString();

            var updateResult = await scope.ResourceStoreAdditions.UpdateApiScopeAsync(retrievedResource);
            updateResult.IsSuccess.Should().BeFalse();
            updateResult.Error.Should().StartWith("Entity not found.");
        }

        [Fact]
        public async Task ShouldUpdateExistingApiScope()
        {
            var testResource = new ApiScope
            {
                Name = "test-name",
                DisplayName = "TestName",
                UserClaims = new List<string> { "claim1", "claim2" },
            };

            (await InitializeServices().ResourceStoreAdditions.CreateApiScopeAsync(testResource))
                .IsSuccess
                .Should()
                .BeTrue();

            var newDisplayName = Guid.NewGuid().ToString();
            var newClaims = new List<string> { "claim4", "claim5" };

            WaitForUserToContinueTheTest(InitializeServices().DocumentStore);

            {
                var scope = InitializeServices();
                var resourceStore = scope.ResourceStore;

                var dbResults = (await resourceStore
                    .FindApiScopesByNameAsync(new[] { testResource.Name })).ToList();
                dbResults.Should().NotBeNull();
                dbResults.Should().ContainSingle();

                var retrievedResource = dbResults.First();
                retrievedResource.DisplayName = newDisplayName;
                retrievedResource.UserClaims = newClaims;

                var updateResult = await scope.ResourceStoreAdditions.UpdateApiScopeAsync(retrievedResource);
                updateResult.IsSuccess.Should().BeTrue();
            }

            {
                var scope = InitializeServices();
                var resourceStore = scope.ResourceStore;

                var dbResults = (await resourceStore
                    .FindApiScopesByNameAsync(new[] { testResource.Name })).ToList();
                dbResults.Should().NotBeNull();
                dbResults.Should().ContainSingle();

                var retrievedResource = dbResults.First();
                retrievedResource.DisplayName.Should().Be(newDisplayName);
                retrievedResource.UserClaims.Should().BeEquivalentTo(newClaims);
                retrievedResource.Should().NotBeEquivalentTo(testResource);
            }
        }

        [Fact]
        public async Task ShouldNotUpdateApiScopeIfEntityUpdatedFromAnotherSession()
        {
            var testResource = new ApiScope
            {
                Name = "test-name",
                DisplayName = "testName",
                UserClaims = new List<string> { "claim1", "claim2" },
            };

            var scope0 = InitializeServices();
            (await scope0.ResourceStoreAdditions.CreateApiScopeAsync(testResource))
                .IsSuccess
                .Should()
                .BeTrue();

            var scope1 = InitializeServices();
            var scope2 = InitializeServices();

            var newDisplayName = Guid.NewGuid().ToString();
            var newClaims = new List<string> { "claim4", "claim5" };

            var dbResults = (await scope1.ResourceStore
                .FindApiScopesByNameAsync(new[] { testResource.Name })).ToList();
            dbResults.Should().NotBeNull();
            dbResults.Should().ContainSingle();

            var retrievedResource = dbResults.First();
            retrievedResource.Should().NotBeNull();

            {
                var dbResults2 = (await scope2.ResourceStore
                    .FindApiScopesByNameAsync(new[] { testResource.Name })).ToList();
                dbResults2.Should().NotBeNull();
                dbResults2.Should().ContainSingle();

                var retrievedResource2 = dbResults2.First();
                retrievedResource2.Should().NotBeNull();

                retrievedResource2.DisplayName = newDisplayName;
                retrievedResource2.UserClaims = newClaims;

                var updateResult2 = await scope2.ResourceStoreAdditions.UpdateApiScopeAsync(retrievedResource2);
                updateResult2.IsSuccess.Should().BeTrue();
            }

            retrievedResource.DisplayName = newDisplayName;
            retrievedResource.UserClaims = newClaims;

            var updateResult = await scope1.ResourceStoreAdditions.UpdateApiScopeAsync(retrievedResource);

            scope0.DocumentSession.Advanced.NumberOfRequests.Should().Be(1, "used to just insert");
            scope2.DocumentSession.Advanced.NumberOfRequests.Should().Be(2, "used to get and update");
            scope1.DocumentSession.Advanced.NumberOfRequests.Should().Be(2, "used to get and fail update");

            updateResult.IsSuccess.Should().BeFalse("resource updated from another process.");
            updateResult.Error.Should().Be(ErrorDescriber.ConcurrencyException);
        }

        [Fact]
        public async Task ShouldDeleteExistingApiScope()
        {
            var scope0 = InitializeServices();
            (await scope0.ResourceStoreAdditions.CreateApiScopeAsync(
                new ApiScope
                {
                    Name = "test-name",
                    UserClaims = new List<string> { "claim1", "claim2" },
                }
            )).IsSuccess.Should().BeTrue();
            (await scope0.ResourceStoreAdditions.CreateApiScopeAsync(
                new ApiScope
                {
                    Name = "test-name-2",
                    UserClaims = new List<string> { "claim1", "claim4" },
                }
            )).IsSuccess.Should().BeTrue();

            {
                var scope2 = InitializeServices();
                var dbResults = (await scope2.ResourceStore
                    .FindApiScopesByNameAsync(new[] { "test-name", "test-name-2" })).ToList();

                dbResults.Should().NotBeNull();
                dbResults.Should().HaveCount(2);

                var retrievedResource = dbResults.First();
                retrievedResource.Should().NotBeNull();

                var retrievedResource2 = dbResults.Last();
                retrievedResource2.Should().NotBeNull();

                (await scope2.ResourceStoreAdditions.DeleteApiScopeAsync("test-name-2")).IsSuccess.Should().BeTrue();
            }

            {
                var scope2 = InitializeServices();
                var dbResults = (await scope2.ResourceStore
                    .FindApiScopesByNameAsync(new[] { "test-name", "test-name-2" })).ToList();

                dbResults.Should().NotBeNull();
                dbResults.Should().ContainSingle();

                var retrievedResource = dbResults.First();
                retrievedResource.Name.Should().Be("test-name");
            }

            var removeNonExistingResult = await InitializeServices()
                .ResourceStoreAdditions
                .DeleteApiScopeAsync("non-existing-name");
            removeNonExistingResult.IsSuccess.Should().BeFalse();
            removeNonExistingResult.Error.Should().StartWith("Entity not found");
        }

        #endregion

        private static IdentityResource CreateIdentityTestResource()
        {
            return new IdentityResource()
            {
                Name = Guid.NewGuid().ToString(),
                DisplayName = Guid.NewGuid().ToString(),
                Description = Guid.NewGuid().ToString(),
                ShowInDiscoveryDocument = true,
                UserClaims =
                {
                    JwtClaimTypes.Subject,
                    JwtClaimTypes.Name,
                },
            };
        }

        private static ApiResource CreateApiResourceTestResource()
        {
            return new ApiResource()
            {
                Name = Guid.NewGuid().ToString(),
                ApiSecrets = new List<Secret> { new Secret("secret".ToSha256()) },
                Scopes = { Guid.NewGuid().ToString() },
                UserClaims =
                {
                    Guid.NewGuid().ToString(),
                    Guid.NewGuid().ToString(),
                },
            };
        }

        private static ApiScope CreateApiScopeTestResource()
        {
            return new ApiScope()
            {
                Name = Guid.NewGuid().ToString(),
                UserClaims =
                {
                    Guid.NewGuid().ToString(),
                    Guid.NewGuid().ToString(),
                },
            };
        }
    }
}