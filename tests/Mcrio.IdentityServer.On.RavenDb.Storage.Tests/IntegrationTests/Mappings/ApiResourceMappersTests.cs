using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Mcrio.IdentityServer.On.RavenDb.Storage.Entities;
using Mcrio.IdentityServer.On.RavenDb.Storage.Mappers;
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
            IIdentityServerStoreMapper mapper = InitializeServices().Mapper;
            mapper.AssertConfigurationIsValid<ApiResourceMapperProfile>();
        }

        [Fact]
        public void Can_Map()
        {
            IIdentityServerStoreMapper mapper = InitializeServices().Mapper;

            var model = new ApiResource();
            Entities.ApiResource mappedEntity = mapper.ToEntity<ApiResource, Entities.ApiResource>(model);
            ApiResource mappedModel = mapper.ToModel<Entities.ApiResource, ApiResource>(mappedEntity);

            Assert.NotNull(mappedModel);
            Assert.NotNull(mappedEntity);
        }

        [Fact]
        public void Properties_Map()
        {
            var model = new ApiResource
            {
                Description = "description",
                DisplayName = "displayname",
                Name = "foo",
                Scopes = { "foo1", "foo2" },
                Enabled = false,
            };

            IIdentityServerStoreMapper mapper = InitializeServices().Mapper;

            Entities.ApiResource? mappedEntity = mapper.ToEntity<ApiResource, Entities.ApiResource>(model);

            mappedEntity.Should().NotBeNull();
            mappedEntity.Scopes.Count.Should().Be(2);

            string? foo1 = mappedEntity.Scopes.FirstOrDefault(x => x == "foo1");
            foo1.Should().NotBeNull();

            string? foo2 = mappedEntity.Scopes.FirstOrDefault(x => x == "foo2");
            foo2.Should().NotBeNull();

            ApiResource mappedModel = mapper.ToModel<Entities.ApiResource, ApiResource>(mappedEntity);

            mappedModel.Description.Should().Be("description");
            mappedModel.DisplayName.Should().Be("displayname");
            mappedModel.Enabled.Should().BeFalse();
            mappedModel.Name.Should().Be("foo");
        }

        [Fact]
        public void missing_values_should_use_defaults()
        {
            var entity = new Entities.ApiResource
            {
                Secrets = new List<ApiResourceSecret>
                {
                    new ApiResourceSecret(),
                },
            };

            var def = new ApiResource
            {
                ApiSecrets = { new Secret("foo") },
            };

            IIdentityServerStoreMapper mapper = InitializeServices().Mapper;

            ApiResource model = mapper.ToModel<Entities.ApiResource, ApiResource>(entity);
            model.ApiSecrets.First().Type.Should().Be(def.ApiSecrets.First().Type);
        }

        [Fact]
        public void ShouldMapSameType()
        {
            var id = Guid.NewGuid().ToString();
            DateTime created = DateTime.UtcNow;
            var entity1 = new Entities.ApiResource()
            {
                Id = id,
                Name = "test",
                Created = created,
            };

            var entity2 = new Entities.ApiResource()
            {
                Id = id,
                Name = "test 22222",
                Created = created,
            };

            IIdentityServerStoreMapper mapper = InitializeServices().Mapper;

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

            IIdentityServerStoreMapper mapper = InitializeServices().Mapper;

            Entities.ApiResource entity = mapper.ToEntity<ApiResource, Entities.ApiResource>(model);
            _output.WriteLine(entity.Id);
            entity.Id.Should().NotBeEmpty();
            entity.Id.Should().EndWith("test-name");
            entity.Id.Should().NotStartWith("test-name");
        }
    }
}