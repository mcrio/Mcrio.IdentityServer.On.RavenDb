using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using IdentityServer4.Models;
using IdentityServer4.Stores;
using Raven.Client;
using Raven.Client.Documents;
using Raven.Client.Documents.Session;
using Xunit;

namespace Mcrio.IdentityServer.On.RavenDb.Storage.Tests.IntegrationTests.Stores
{
    public class PersistedGrantStoreTest : IntegrationTestBase
    {
        [Fact]
        public async Task StoreAsync_WhenPersistedGrantStored_ExpectSuccess()
        {
            var persistedGrant = CreateTestObject();
            await InitializeServices().PersistedGrantStore.StoreAsync(persistedGrant);

            List<Entities.PersistedGrant> grants = await InitializeServices().DocumentSession
                .Query<Entities.PersistedGrant>()
                .ToListAsync();
            grants.Should().ContainSingle();

            var foundGrant = grants.First();
            foundGrant.Should().NotBeNull();
            foundGrant.Should().BeEquivalentTo(persistedGrant);
        }

        [Fact]
        public async Task GetAsync_WithKeyAndPersistedGrantExists_ExpectPersistedGrantReturned()
        {
            var persistedGrant = CreateTestObject();
            await InitializeServices().PersistedGrantStore.StoreAsync(persistedGrant);

            PersistedGrant foundPersistedGrant = await InitializeServices()
                .PersistedGrantStore
                .GetAsync(persistedGrant.Key);

            foundPersistedGrant.Should().NotBeNull();
            foundPersistedGrant.Should().BeEquivalentTo(persistedGrant);
        }

        [Fact]
        public async Task GetAllAsync_WithSubAndTypeAndPersistedGrantExists_ExpectPersistedGrantReturned()
        {
            var persistedGrant = CreateTestObject();
            await InitializeServices().PersistedGrantStore.StoreAsync(persistedGrant);

            IList<PersistedGrant> foundPersistedGrants = (
                await InitializeServices()
                    .PersistedGrantStore
                    .GetAllAsync(new PersistedGrantFilter { SubjectId = persistedGrant.SubjectId })
            ).ToList();

            foundPersistedGrants.Should().NotBeNull();
            foundPersistedGrants.Should().NotBeEmpty();
            foundPersistedGrants.First().Should().BeEquivalentTo(persistedGrant);
        }

        [Fact]
        public async Task GetAllAsync_Should_Filter()
        {
            await InitializeServices().PersistedGrantStore.StoreAsync(
                CreateTestObject(sub: "sub1", clientId: "c1", sid: "s1", type: "t1")
            );
            await InitializeServices().PersistedGrantStore.StoreAsync(
                CreateTestObject(sub: "sub1", clientId: "c1", sid: "s1", type: "t2")
            );
            await InitializeServices().PersistedGrantStore.StoreAsync(
                CreateTestObject(sub: "sub1", clientId: "c1", sid: "s2", type: "t1")
            );
            await InitializeServices().PersistedGrantStore.StoreAsync(
                CreateTestObject(sub: "sub1", clientId: "c1", sid: "s2", type: "t2")
            );
            await InitializeServices().PersistedGrantStore.StoreAsync(
                CreateTestObject(sub: "sub1", clientId: "c2", sid: "s1", type: "t1")
            );
            await InitializeServices().PersistedGrantStore.StoreAsync(
                CreateTestObject(sub: "sub1", clientId: "c2", sid: "s1", type: "t2")
            );
            await InitializeServices().PersistedGrantStore.StoreAsync(
                CreateTestObject(sub: "sub1", clientId: "c2", sid: "s2", type: "t1")
            );
            await InitializeServices().PersistedGrantStore.StoreAsync(
                CreateTestObject(sub: "sub1", clientId: "c2", sid: "s2", type: "t2")
            );
            await InitializeServices().PersistedGrantStore.StoreAsync(
                CreateTestObject(sub: "sub1", clientId: "c3", sid: "s3", type: "t3")
            );
            await InitializeServices().PersistedGrantStore.StoreAsync(
                CreateTestObject()
            );

            WaitForIndexing(InitializeServices().DocumentStore);

            var store = InitializeServices().PersistedGrantStore;

            (await store.GetAllAsync(new PersistedGrantFilter
            {
                SubjectId = "sub1",
            })).ToList().Count.Should().Be(9);
            (await store.GetAllAsync(new PersistedGrantFilter
            {
                SubjectId = "sub2",
            })).ToList().Count.Should().Be(0);
            (await store.GetAllAsync(new PersistedGrantFilter
            {
                SubjectId = "sub1",
                ClientId = "c1",
            })).ToList().Count.Should().Be(4);
            (await store.GetAllAsync(new PersistedGrantFilter
            {
                SubjectId = "sub1",
                ClientId = "c2",
            })).ToList().Count.Should().Be(4);
            (await store.GetAllAsync(new PersistedGrantFilter
            {
                SubjectId = "sub1",
                ClientId = "c3",
            })).ToList().Count.Should().Be(1);
            (await store.GetAllAsync(new PersistedGrantFilter
            {
                SubjectId = "sub1",
                ClientId = "c4",
            })).ToList().Count.Should().Be(0);
            (await store.GetAllAsync(new PersistedGrantFilter
            {
                SubjectId = "sub1",
                ClientId = "c1",
                SessionId = "s1",
            })).ToList().Count.Should().Be(2);
            (await store.GetAllAsync(new PersistedGrantFilter
            {
                SubjectId = "sub1",
                ClientId = "c3",
                SessionId = "s1",
            })).ToList().Count.Should().Be(0);
            (await store.GetAllAsync(new PersistedGrantFilter
            {
                SubjectId = "sub1",
                ClientId = "c1",
                SessionId = "s1",
                Type = "t1",
            })).ToList().Count.Should().Be(1);
            (await store.GetAllAsync(new PersistedGrantFilter
            {
                SubjectId = "sub1",
                ClientId = "c1",
                SessionId = "s1",
                Type = "t3",
            })).ToList().Count.Should().Be(0);
        }

        [Fact]
        public async Task RemoveAsync_WhenKeyOfExistingReceived_ExpectGrantDeleted()
        {
            var persistedGrant = CreateTestObject();
            await InitializeServices().PersistedGrantStore.StoreAsync(persistedGrant);

            {
                Entities.PersistedGrant foundGrant = await InitializeServices()
                    .DocumentSession
                    .Query<Entities.PersistedGrant>()
                    .SingleOrDefaultAsync(x => x.Key == persistedGrant.Key);

                foundGrant.Should().NotBeNull();
            }

            await InitializeServices().PersistedGrantStore.RemoveAsync(persistedGrant.Key);

            {
                Entities.PersistedGrant foundGrant = await InitializeServices()
                    .DocumentSession
                    .Query<Entities.PersistedGrant>()
                    .SingleOrDefaultAsync(x => x.Key == persistedGrant.Key);

                foundGrant.Should().BeNull();
            }
        }

        [Fact]
        public async Task RemoveAllAsync_WhenSubIdAndClientIdOfExistingReceived_ExpectGrantDeleted()
        {
            var persistedGrant = CreateTestObject();
            await InitializeServices().PersistedGrantStore.StoreAsync(persistedGrant);

            await InitializeServices().PersistedGrantStore.RemoveAllAsync(new PersistedGrantFilter
            {
                SubjectId = persistedGrant.SubjectId,
                ClientId = persistedGrant.ClientId,
            });

            WaitForIndexing(InitializeServices().DocumentStore);
            Entities.PersistedGrant foundGrant = await InitializeServices()
                .DocumentSession
                .Query<Entities.PersistedGrant>()
                .FirstOrDefaultAsync(x => x.Key == persistedGrant.Key);
            Assert.Null(foundGrant);
        }

        [Fact]
        public async Task RemoveAllAsync_WhenSubIdClientIdAndTypeOfExistingReceived_ExpectGrantDeleted()
        {
            var persistedGrant = CreateTestObject();
            await InitializeServices().PersistedGrantStore.StoreAsync(persistedGrant);

            WaitForIndexing(InitializeServices().DocumentStore);
            await InitializeServices().PersistedGrantStore.RemoveAllAsync(new PersistedGrantFilter
            {
                SubjectId = persistedGrant.SubjectId,
                ClientId = persistedGrant.ClientId,
                Type = persistedGrant.Type,
            });

            WaitForIndexing(InitializeServices().DocumentStore);
            Entities.PersistedGrant foundGrant = await InitializeServices()
                .DocumentSession
                .Query<Entities.PersistedGrant>()
                .FirstOrDefaultAsync(x => x.Key == persistedGrant.Key);
            Assert.Null(foundGrant);
        }

        // ReSharper disable once SA1201
        public static IEnumerable<object[]> RemoveAllAsyncShouldFilterData =>
            new List<object[]>
            {
                new object[]
                {
                    new PersistedGrantFilter
                    {
                        SubjectId = "sub1",
                    },
                    1,
                },
                new object[]
                {
                    new PersistedGrantFilter
                    {
                        SubjectId = "sub2",
                    },
                    10,
                },
                new object[]
                {
                    new PersistedGrantFilter
                    {
                        SubjectId = "sub1",
                        ClientId = "c1",
                    },
                    6,
                },
                new object[]
                {
                    new PersistedGrantFilter
                    {
                        SubjectId = "sub1",
                        ClientId = "c2",
                    },
                    6,
                },
                new object[]
                {
                    new PersistedGrantFilter
                    {
                        SubjectId = "sub1",
                        ClientId = "c3",
                    },
                    9,
                },
                new object[]
                {
                    new PersistedGrantFilter
                    {
                        SubjectId = "sub1",
                        ClientId = "c4",
                    },
                    10,
                },
                new object[]
                {
                    new PersistedGrantFilter
                    {
                        SubjectId = "sub1",
                        ClientId = "c1",
                        SessionId = "s1",
                    },
                    8,
                },
                new object[]
                {
                    new PersistedGrantFilter
                    {
                        SubjectId = "sub1",
                        ClientId = "c3",
                        SessionId = "s1",
                    },
                    10,
                },
                new object[]
                {
                    new PersistedGrantFilter
                    {
                        SubjectId = "sub1",
                        ClientId = "c1",
                        SessionId = "s1",
                        Type = "t1",
                    },
                    9,
                },
                new object[]
                {
                    new PersistedGrantFilter
                    {
                        SubjectId = "sub1",
                        ClientId = "c1",
                        SessionId = "s1",
                        Type = "t3",
                    },
                    10,
                },
            };

        [Theory]
        [MemberData(nameof(RemoveAllAsyncShouldFilterData))]
        public async Task RemoveAllAsync_Should_Filter(PersistedGrantFilter filter, int expectedCountAfterDelete)
        {
            async Task PopulateDbAsync()
            {
                var scope = InitializeServices();
                var store = scope.PersistedGrantStore;
                await store.StoreAsync(CreateTestObject(sub: "sub1", clientId: "c1", sid: "s1", type: "t1"));
                await store.StoreAsync(CreateTestObject(sub: "sub1", clientId: "c1", sid: "s1", type: "t2"));
                await store.StoreAsync(CreateTestObject(sub: "sub1", clientId: "c1", sid: "s2", type: "t1"));
                await store.StoreAsync(CreateTestObject(sub: "sub1", clientId: "c1", sid: "s2", type: "t2"));
                await store.StoreAsync(CreateTestObject(sub: "sub1", clientId: "c2", sid: "s1", type: "t1"));
                await store.StoreAsync(CreateTestObject(sub: "sub1", clientId: "c2", sid: "s1", type: "t2"));
                await store.StoreAsync(CreateTestObject(sub: "sub1", clientId: "c2", sid: "s2", type: "t1"));
                await store.StoreAsync(CreateTestObject(sub: "sub1", clientId: "c2", sid: "s2", type: "t2"));
                await store.StoreAsync(CreateTestObject(sub: "sub1", clientId: "c3", sid: "s3", type: "t3"));
                await store.StoreAsync(CreateTestObject());
            }

            await PopulateDbAsync();
            {
                var scope = InitializeServices();
                WaitForIndexing(scope.DocumentStore);
                await scope.PersistedGrantStore.RemoveAllAsync(filter);
                WaitForIndexing(scope.DocumentStore);
                int grantsCount = await scope.DocumentSession.Query<Entities.PersistedGrant>().CountAsync();
                grantsCount.Should().Be(expectedCountAfterDelete);
            }
        }

        [Fact]
        public async Task Store_should_create_new_record_if_key_does_not_exist()
        {
            var persistedGrant = CreateTestObject();

            {
                Entities.PersistedGrant foundGrant = await InitializeServices()
                    .DocumentSession
                    .Query<Entities.PersistedGrant>()
                    .FirstOrDefaultAsync(x => x.Key == persistedGrant.Key);
                Assert.Null(foundGrant);
            }

            await InitializeServices().PersistedGrantStore.StoreAsync(persistedGrant);
            WaitForIndexing(InitializeServices().DocumentStore);

            {
                Entities.PersistedGrant foundGrant = await InitializeServices()
                    .DocumentSession
                    .Query<Entities.PersistedGrant>()
                    .FirstOrDefaultAsync(x => x.Key == persistedGrant.Key);
                Assert.NotNull(foundGrant);
            }
        }

        [Fact]
        public async Task Store_should_update_record_if_key_already_exists()
        {
            PersistedGrant persistedGrant = CreateTestObject();
            await InitializeServices().PersistedGrantStore.StoreAsync(persistedGrant);

            DateTime newDate = persistedGrant.Expiration!.Value.AddHours(1);
            persistedGrant.Expiration = newDate;
            await InitializeServices().PersistedGrantStore.StoreAsync(persistedGrant);

            {
                Entities.PersistedGrant foundGrant = await InitializeServices()
                    .DocumentSession
                    .Query<Entities.PersistedGrant>()
                    .FirstOrDefaultAsync(x => x.Key == persistedGrant.Key);
                Assert.NotNull(foundGrant);
                Assert.Equal(newDate, persistedGrant.Expiration);
            }
        }

        [Fact]
        public async Task ShouldAddAndUpdateExpiresDocumentMetadataIfOptionEnabled()
        {
            PersistedGrant persistedGrant = CreateTestObject();
            await InitializeServices(
                tokenOptions => tokenOptions.SetRavenDbDocumentExpiresMetadata = true
            ).PersistedGrantStore.StoreAsync(persistedGrant);

            {
                ServiceScope scope = InitializeServices();
                Entities.PersistedGrant foundGrant = await scope
                    .DocumentSession
                    .Query<Entities.PersistedGrant>()
                    .FirstOrDefaultAsync(x => x.Key == persistedGrant.Key);
                foundGrant.Should().NotBeNull();
                IMetadataDictionary metadata = scope.DocumentSession.Advanced.GetMetadataFor(foundGrant);
                metadata.Should().ContainKey(Constants.Documents.Metadata.Expires);
                metadata[Constants.Documents.Metadata.Expires]
                    .Should()
                    .Be(persistedGrant.Expiration!.Value.ToUniversalTime().ToString("O"));
            }

            var newExpiryDate = new DateTime(2021, 08, 20, 0, 0, 0, DateTimeKind.Utc);
            persistedGrant.Expiration = newExpiryDate;
            await InitializeServices(
                tokenOptions => tokenOptions.SetRavenDbDocumentExpiresMetadata = true
            ).PersistedGrantStore.StoreAsync(persistedGrant);

            {
                ServiceScope scope = InitializeServices();
                Entities.PersistedGrant foundGrant = await scope
                    .DocumentSession
                    .Query<Entities.PersistedGrant>()
                    .FirstOrDefaultAsync(x => x.Key == persistedGrant.Key);
                foundGrant.Should().NotBeNull();
                IMetadataDictionary metadata = scope.DocumentSession.Advanced.GetMetadataFor(foundGrant);
                metadata.Should().ContainKey(Constants.Documents.Metadata.Expires);
                metadata[Constants.Documents.Metadata.Expires]
                    .Should()
                    .Be(newExpiryDate.ToUniversalTime().ToString("O"));
            }

            persistedGrant.Expiration = null;
            await InitializeServices(
                tokenOptions => tokenOptions.SetRavenDbDocumentExpiresMetadata = true
            ).PersistedGrantStore.StoreAsync(persistedGrant);

            {
                ServiceScope scope = InitializeServices();
                Entities.PersistedGrant foundGrant = await scope
                    .DocumentSession
                    .Query<Entities.PersistedGrant>()
                    .FirstOrDefaultAsync(x => x.Key == persistedGrant.Key);
                foundGrant.Should().NotBeNull();
                IMetadataDictionary metadata = scope.DocumentSession.Advanced.GetMetadataFor(foundGrant);
                metadata.Should().NotContainKey(Constants.Documents.Metadata.Expires);
            }
        }

        [Fact]
        public async Task ShouldNotAddExpiresDocumentMetadataOnStoreAndUpdateWhenOptionDisabled()
        {
            PersistedGrant persistedGrant = CreateTestObject();
            await InitializeServices(
                tokenOptions => tokenOptions.SetRavenDbDocumentExpiresMetadata = false
            ).PersistedGrantStore.StoreAsync(persistedGrant);

            {
                ServiceScope scope = InitializeServices();
                Entities.PersistedGrant foundGrant = await scope
                    .DocumentSession
                    .Query<Entities.PersistedGrant>()
                    .FirstOrDefaultAsync(x => x.Key == persistedGrant.Key);
                foundGrant.Should().NotBeNull();
                IMetadataDictionary metadata = scope.DocumentSession.Advanced.GetMetadataFor(foundGrant);
                metadata.Should().NotContainKey(
                    Constants.Documents.Metadata.Expires,
                    "token clean-up options disabled storing @expires metadata"
                );
            }

            var newExpiryDate = new DateTime(2021, 08, 20, 0, 0, 0, DateTimeKind.Utc);
            persistedGrant.Expiration = newExpiryDate;
            await InitializeServices(
                tokenOptions => tokenOptions.SetRavenDbDocumentExpiresMetadata = false
            ).PersistedGrantStore.StoreAsync(persistedGrant);

            {
                ServiceScope scope = InitializeServices();
                Entities.PersistedGrant foundGrant = await scope
                    .DocumentSession
                    .Query<Entities.PersistedGrant>()
                    .FirstOrDefaultAsync(x => x.Key == persistedGrant.Key);
                foundGrant.Should().NotBeNull();
                IMetadataDictionary metadata = scope.DocumentSession.Advanced.GetMetadataFor(foundGrant);
                metadata.Should().NotContainKey(Constants.Documents.Metadata.Expires);
            }
        }

        [Fact]
        public async Task ShouldNotUpdatePersistedGrantIfEntityUpdatedFromAnotherSession()
        {
            PersistedGrant persistedGrant = CreateTestObject();
            PersistedGrant persistedGrant2 = CreateTestObject();
            persistedGrant2.ConsumedTime = null;
            await InitializeServices().PersistedGrantStore.StoreAsync(persistedGrant);
            await InitializeServices().PersistedGrantStore.StoreAsync(persistedGrant2);

            DateTime? originalConsumedTime = persistedGrant.ConsumedTime;

            ServiceScope scope1 = InitializeServices();
            ServiceScope scope2 = InitializeServices();

            // have 2 grants retrieved in scope 1
            PersistedGrant? grantFromScope1 = await scope1.PersistedGrantStore.GetAsync(persistedGrant.Key);
            grantFromScope1.Should().NotBeNull();
            PersistedGrant? grant2FromScope1 = await scope1.PersistedGrantStore.GetAsync(persistedGrant2.Key);

            // retrieve 1 grant in scope
            PersistedGrant? grantFromScope2 = await scope2.PersistedGrantStore.GetAsync(persistedGrant.Key);
            grantFromScope2.Should().NotBeNull();

            // modify scope2
            DateTime newConsumedTime = DateTime.Now;
            grantFromScope2.ConsumedTime = newConsumedTime;
            await scope2.PersistedGrantStore.StoreAsync(grantFromScope2);

            // try modifying data from scope1.
            grantFromScope1.ConsumedTime = DateTime.Now.Subtract(TimeSpan.FromHours(3));
            grant2FromScope1.ConsumedTime = DateTime.Now;
            await scope1.PersistedGrantStore.StoreAsync(grantFromScope1); // will call saveChanges

            // make sure data modifications from scope1 are not saved due to concurrency
            ServiceScope scope3 = InitializeServices();
            PersistedGrant? updatedGrant = await scope3.PersistedGrantStore.GetAsync(persistedGrant.Key);
            updatedGrant.Should().NotBeNull();
            updatedGrant.ConsumedTime.Should().Be(newConsumedTime);
            updatedGrant.ConsumedTime.Should().NotBe(grantFromScope1.ConsumedTime.Value);
            if (originalConsumedTime != null)
            {
                updatedGrant.ConsumedTime.Should().NotBe(originalConsumedTime.Value);
            }

            PersistedGrant? grant2RetrievedAgain = await scope3.PersistedGrantStore.GetAsync(persistedGrant2.Key);
            grant2RetrievedAgain.Should().NotBeNull();
            grant2RetrievedAgain.ConsumedTime.Should().BeNull();
        }

        private static PersistedGrant CreateTestObject(
            string sub = null,
            string clientId = null,
            string sid = null,
            string type = null)
        {
            return new PersistedGrant
            {
                Key = Guid.NewGuid().ToString(),
                Type = type ?? "authorization_code",
                ClientId = clientId ?? Guid.NewGuid().ToString(),
                SubjectId = sub ?? Guid.NewGuid().ToString(),
                SessionId = sid ?? Guid.NewGuid().ToString(),
                CreationTime = new DateTime(2016, 08, 01, 0, 0, 0, DateTimeKind.Utc),
                Expiration = new DateTime(2016, 08, 31, 0, 0, 0, DateTimeKind.Utc),
                Data = Guid.NewGuid().ToString(),
            };
        }
    }
}