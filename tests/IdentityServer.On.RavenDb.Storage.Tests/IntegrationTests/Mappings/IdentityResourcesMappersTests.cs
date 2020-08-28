using System;
using FluentAssertions;
using IdentityServer4.Models;
using Mcrio.IdentityServer.On.RavenDb.Storage.Mappers;
using Mcrio.IdentityServer.On.RavenDb.Storage.Mappers.Profiles;
using Xunit;
using Xunit.Abstractions;

namespace Mcrio.IdentityServer.On.RavenDb.Storage.Tests.IntegrationTests.Mappings
{
    public class IdentityResourcesMappersTests : IntegrationTestBase
    {
        private readonly ITestOutputHelper _output;

        public IdentityResourcesMappersTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void IdentityResourceAutomapperConfigurationIsValid()
        {
            IIdentityServerStoreMapper mapper = InitializeServices().Mapper;
            mapper.AssertConfigurationIsValid<IdentityResourceMapperProfile>();
        }

        [Fact]
        public void CanMapIdentityResources()
        {
            IIdentityServerStoreMapper mapper = InitializeServices().Mapper;

            var model = new IdentityResource();
            Entities.IdentityResource mappedEntity =
                mapper.ToEntity<IdentityResource, Entities.IdentityResource>(model);
            IdentityResource mappedModel = mapper.ToModel<Entities.IdentityResource, IdentityResource>(mappedEntity);

            Assert.NotNull(mappedModel);
            Assert.NotNull(mappedEntity);
        }

        [Fact]
        public void ShouldMapSameType()
        {
            var id = Guid.NewGuid().ToString();
            DateTime created = DateTime.UtcNow;
            var entity1 = new Entities.IdentityResource()
            {
                Id = id,
                Name = "test",
                Created = created,
            };

            var entity2 = new Entities.IdentityResource()
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
        public void ShouldMapIdentityResourceNameToEntityId()
        {
            var model = new IdentityResource()
            {
                Name = "test-name",
            };

            IIdentityServerStoreMapper mapper = InitializeServices().Mapper;

            Entities.IdentityResource entity = mapper.ToEntity<IdentityResource, Entities.IdentityResource>(model);
            _output.WriteLine(entity.Id);
            entity.Id.Should().NotBeEmpty();
            entity.Id.Should().EndWith("test-name");
            entity.Id.Should().NotStartWith("test-name");
        }
    }
}