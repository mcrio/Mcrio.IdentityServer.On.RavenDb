using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Mcrio.IdentityServer.On.RavenDb.Storage.Entities;
using Mcrio.IdentityServer.On.RavenDb.Storage.Mappers.Profiles;
using Xunit;
using Xunit.Abstractions;
using Client = IdentityServer4.Models.Client;
using Secret = IdentityServer4.Models.Secret;

namespace Mcrio.IdentityServer.On.RavenDb.Storage.Tests.IntegrationTests.Mappings
{
    public class ClientMappersTests : IntegrationTestBase
    {
        private readonly ITestOutputHelper _output;

        public ClientMappersTests(ITestOutputHelper output)
        {
            _output = output;
        }
        [Fact]
        public void AutomapperConfigurationIsValid()
        {
            var mapper = InitializeServices().Mapper;
            mapper.AssertConfigurationIsValid<ClientMapperProfile>();
        }

        [Fact]
        public void Can_Map()
        {
            var mapper = InitializeServices().Mapper;

            var model = new Client();
            var mappedEntity = mapper.ToEntity(model);
            var mappedModel = mapper.ToModel(mappedEntity);

            Assert.NotNull(mappedModel);
            Assert.NotNull(mappedEntity);
        }

        [Fact]
        public void Properties_Map()
        {
            var model = new Client()
            {
                Properties =
                {
                    { "foo1", "bar1" },
                    { "foo2", "bar2" },
                }
            };

            var mapper = InitializeServices().Mapper;

            var mappedEntity = mapper.ToEntity(model);

            mappedEntity.Properties.Count.Should().Be(2);
            var foo1 = mappedEntity.Properties.FirstOrDefault(x => x.Key == "foo1");
            foo1.Should().NotBeNull();
            foo1.Value.Should().Be("bar1");
            var foo2 = mappedEntity.Properties.FirstOrDefault(x => x.Key == "foo2");
            foo2.Should().NotBeNull();
            foo2.Value.Should().Be("bar2");


            var mappedModel = mapper.ToModel(mappedEntity);

            mappedModel.Properties.Count.Should().Be(2);
            mappedModel.Properties.ContainsKey("foo1").Should().BeTrue();
            mappedModel.Properties.ContainsKey("foo2").Should().BeTrue();
            mappedModel.Properties["foo1"].Should().Be("bar1");
            mappedModel.Properties["foo2"].Should().Be("bar2");
        }

        [Fact]
        public void duplicates_properties_in_db_map()
        {
            Action modelAction = () =>
            {
                var entity = new RavenDb.Storage.Entities.Client
                {
                    Properties = new Dictionary<string, string>()
                    {
                        { "foo1", "bar1" },
                        { "foo1", "bar2" }
                    }
                };
                var mapper = InitializeServices().Mapper;
                mapper.ToModel(entity);
            };
            modelAction.Should().Throw<Exception>();
        }

        [Fact]
        public void missing_values_should_use_defaults()
        {
            var entity = new RavenDb.Storage.Entities.Client
            {
                ClientSecrets = new List<ClientSecret>
                {
                    new ClientSecret
                    {
                    }
                }
            };

            var def = new Client
            {
                ClientSecrets = { new Secret("foo") },
            };

            var mapper = InitializeServices().Mapper;

            var model = mapper.ToModel(entity);
            model.ProtocolType.Should().Be(def.ProtocolType);
            model.ClientSecrets.First().Type.Should().Be(def.ClientSecrets.First().Type);
        }

        [Fact]
        public void ShouldMapSameType()
        {
            var id = Guid.NewGuid().ToString();
            DateTime created = DateTime.UtcNow;
            var entity1 = new RavenDb.Storage.Entities.Client
            {
                Id = id,
                ClientName = "test",
                Created = created,
                Claims = new List<ClientClaim> { new ClientClaim { Type = "foo", Value = "bar" } },
            };

            var entity2 = new RavenDb.Storage.Entities.Client
            {
                Id = id,
                ClientName = "test 22222",
                Created = created,
                Claims = new List<ClientClaim>(),
            };

            var mapper = InitializeServices().Mapper;

            entity1.Should().NotBeEquivalentTo(entity2);
            mapper.Map(entity1, entity2);

            entity1.Should().BeEquivalentTo(entity2);
            entity1.Should().NotBeSameAs(entity2, "not the same instance");
        }

        [Fact]
        public void ShouldMapClientIdToEntityId()
        {
            var model = new Client()
            {
                ClientId = "test-client",
            };

            var mapper = InitializeServices().Mapper;

            RavenDb.Storage.Entities.Client entity = mapper.ToEntity(model);
            _output.WriteLine(entity.Id);
            entity.Id.Should().NotBeEmpty();
            entity.Id.Should().EndWith("test-client");
            entity.Id.Should().NotStartWith("test-client");
        }
    }
}