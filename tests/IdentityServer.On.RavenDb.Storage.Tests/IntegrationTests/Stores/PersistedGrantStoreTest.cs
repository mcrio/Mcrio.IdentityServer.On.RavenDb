using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using IdentityServer4.Models;
using IdentityServer4.Stores;
using Raven.Client.Documents;
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
            var persistedGrant = CreateTestObject();
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
                CreationTime = new DateTime(2016, 08, 01),
                Expiration = new DateTime(2016, 08, 31),
                Data = Guid.NewGuid().ToString(),
            };
        }
    }
}