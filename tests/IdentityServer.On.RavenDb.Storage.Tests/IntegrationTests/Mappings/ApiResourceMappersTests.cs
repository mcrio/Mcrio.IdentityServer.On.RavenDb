using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Mcrio.IdentityServer.On.RavenDb.Storage.Entities;
using Mcrio.IdentityServer.On.RavenDb.Storage.Mappers.Profiles;
using Xunit;
using Xunit.Abstractions;
using ApiResource = IdentityServer4.Models.ApiResource;
using Secret = IdentityServer4.Models.Secret;

namespace Mcrio.IdentityServer.On.RavenDb.Storage.Tests.IntegrationTests.Mappings
{
    public class ApiResourceMappersTests : IntegrationTestBase
    {
        private readonly ITestOutputHelper _output;

        public ApiResourceMappersTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void AutomapperConfigurationIsValid()
        {
            var mapper = InitializeServices().Mapper;
            mapper.AssertConfigurationIsValid<ApiResourceMapperProfile>();
        }

        [Fact]
        public void Can_Map()
        {
            var mapper = InitializeServices().Mapper;

            var model = new ApiResource();
            var mappedEntity = mapper.ToEntity(model);
            var mappedModel = mapper.ToModel(mappedEntity);

            Assert.NotNull(mappedModel);
            Assert.NotNull(mappedEntity);
        }

        [Fact]
        public void Properties_Map()
        {
            var model = new ApiResource()
            {
                Description = "description",
                DisplayName = "displayname",
                Name = "foo",
                Scopes = { "foo1", "foo2" },
                Enabled = false,
            };

            var mapper = InitializeServices().Mapper;

            RavenDb.Storage.Entities.ApiResource? mappedEntity = mapper.ToEntity(model);

            mappedEntity.Should().NotBeNull();
            mappedEntity.Scopes.Count.Should().Be(2);
            var foo1 = mappedEntity.Scopes.FirstOrDefault(x => x == "foo1");
            foo1.Should().NotBeNull();
            var foo2 = mappedEntity.Scopes.FirstOrDefault(x => x == "foo2");
            foo2.Should().NotBeNull();

            var mappedModel = mapper.ToModel(mappedEntity);

            mappedModel.Description.Should().Be("description");
            mappedModel.DisplayName.Should().Be("displayname");
            mappedModel.Enabled.Should().BeFalse();
            mappedModel.Name.Should().Be("foo");
        }

        [Fact]
        public void missing_values_should_use_defaults()
        {
            var entity = new RavenDb.Storage.Entities.ApiResource
            {
                Secrets = new List<ApiResourceSecret>
                {
                    new ApiResourceSecret
                    {
                    }
                }
            };

            var def = new ApiResource
            {
                ApiSecrets = { new Secret("foo") }
            };

            var mapper = InitializeServices().Mapper;

            var model = mapper.ToModel(entity);
            model.ApiSecrets.First().Type.Should().Be(def.ApiSecrets.First().Type);
        }

        [Fact]
        public void ShouldMapSameType()
        {
            var id = Guid.NewGuid().ToString();
            DateTime created = DateTime.UtcNow;
            var entity1 = new RavenDb.Storage.Entities.ApiResource()
            {
                Id = id,
                Name = "test",
                Created = created,
            };

            var entity2 = new RavenDb.Storage.Entities.ApiResource()
            {
                Id = id,
                Name = "test 22222",
                Created = created,
            };

            var mapper = InitializeServices().Mapper;

            entity1.Should().NotBeEquivalentTo(entity2);
            mapper.Map(entity1, entity2);

            entity1.Should().BeEquivalentTo(entity2);
            entity1.Should().NotBeSameAs(entity2, "not the same instance");
        }

        [Fact]
        public void ShouldMapApiResourceNameToEntityId()
        {
            var model = new ApiResource()
            {
                Name = "test-name",
            };

            var mapper = InitializeServices().Mapper;

            RavenDb.Storage.Entities.ApiResource entity = mapper.ToEntity(model);
            _output.WriteLine(entity.Id);
            entity.Id.Should().NotBeEmpty();
            entity.Id.Should().EndWith("test-name");
            entity.Id.Should().NotStartWith("test-name");
        }
    }
}