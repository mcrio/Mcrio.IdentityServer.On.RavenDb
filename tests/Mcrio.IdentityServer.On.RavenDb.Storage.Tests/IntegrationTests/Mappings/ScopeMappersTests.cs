using System;
using FluentAssertions;
using IdentityServer4.Models;
using Mcrio.IdentityServer.On.RavenDb.Storage.Mappers;
using Mcrio.IdentityServer.On.RavenDb.Storage.Mappers.Profiles;
using Xunit;
using Xunit.Abstractions;

namespace Mcrio.IdentityServer.On.RavenDb.Storage.Tests.IntegrationTests.Mappings
{
    public class ScopesMappersTests : IntegrationTestBase
    {
        private readonly ITestOutputHelper _output;

        public ScopesMappersTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void ScopeAutomapperConfigurationIsValid()
        {
            IIdentityServerStoreMapper mapper = InitializeServices().Mapper;
            mapper.AssertConfigurationIsValid<ScopeMapperProfile>();
        }

        [Fact]
        public void CanMapScope()
        {
            IIdentityServerStoreMapper mapper = InitializeServices().Mapper;

            var model = new ApiScope();
            Entities.ApiScope mappedEntity = mapper.ToEntity<ApiScope, Entities.ApiScope>(model);
            ApiScope mappedModel = mapper.ToModel<Entities.ApiScope, ApiScope>(mappedEntity);

            Assert.NotNull(mappedModel);
            Assert.NotNull(mappedEntity);
        }

        [Fact]
        public void Properties_Map()
        {
            var model = new ApiScope
            {
                Description = "description",
                DisplayName = "displayname",
                Name = "foo",
                UserClaims = { "c1", "c2" },
                Properties =
                {
                    { "x", "xx" },
                    { "y", "yy" },
                },
                Enabled = false,
            };

            IIdentityServerStoreMapper mapper = InitializeServices().Mapper;

            Entities.ApiScope mappedEntity = mapper.ToEntity<ApiScope, Entities.ApiScope>(model);
            mappedEntity.Description.Should().Be("description");
            mappedEntity.DisplayName.Should().Be("displayname");
            mappedEntity.Name.Should().Be("foo");

            mappedEntity.UserClaims.Count.Should().Be(2);
            mappedEntity.UserClaims.Should().BeEquivalentTo(new[] { "c1", "c2" });
            mappedEntity.Properties.Count.Should().Be(2);
            mappedEntity.Properties.Should().Contain("x", "xx");
            mappedEntity.Properties.Should().Contain("y", "yy");

            ApiScope mappedModel = mapper.ToModel<Entities.ApiScope, ApiScope>(mappedEntity);

            mappedModel.Description.Should().Be("description");
            mappedModel.DisplayName.Should().Be("displayname");
            mappedModel.Enabled.Should().BeFalse();
            mappedModel.Name.Should().Be("foo");
            mappedModel.UserClaims.Count.Should().Be(2);
            mappedModel.UserClaims.Should().BeEquivalentTo(new[] { "c1", "c2" });
            mappedModel.Properties.Count.Should().Be(2);
            mappedModel.Properties["x"].Should().Be("xx");
            mappedModel.Properties["y"].Should().Be("yy");
        }

        [Fact]
        public void ShouldMapSameType()
        {
            var id = Guid.NewGuid().ToString();
            DateTime created = DateTime.UtcNow;
            var entity1 = new Entities.ApiScope()
            {
                Id = id,
                Name = "test",
            };

            var entity2 = new Entities.ApiScope()
            {
                Id = id,
                Name = "test 22222",
            };

            IIdentityServerStoreMapper mapper = InitializeServices().Mapper;

            entity1.Should().NotBeEquivalentTo(entity2);
            mapper.Map(entity1, entity2);

            entity1.Should().BeEquivalentTo(entity2);
            entity1.Should().NotBeSameAs(entity2, "not the same instance");
        }

        [Fact]
        public void ShouldMapApiScopeNameToEntityId()
        {
            var model = new ApiScope()
            {
                Name = "test-name",
            };

            IIdentityServerStoreMapper mapper = InitializeServices().Mapper;

            Entities.ApiScope entity = mapper.ToEntity<ApiScope, Entities.ApiScope>(model);
            _output.WriteLine(entity.Id);
            entity.Id.Should().NotBeEmpty();
            entity.Id.Should().EndWith("test-name");
            entity.Id.Should().NotStartWith("test-name");
        }
    }
}