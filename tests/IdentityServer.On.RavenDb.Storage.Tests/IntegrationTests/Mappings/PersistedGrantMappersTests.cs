using System;
using FluentAssertions;
using IdentityServer4.Models;
using Mcrio.IdentityServer.On.RavenDb.Storage.Mappers.Profiles;
using Xunit;
using Xunit.Abstractions;

namespace Mcrio.IdentityServer.On.RavenDb.Storage.Tests.IntegrationTests.Mappings
{
    public class PersistedGrantMappersTests : IntegrationTestBase
    {
        private readonly ITestOutputHelper _output;

        public PersistedGrantMappersTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void PersistedGrantAutomapperConfigurationIsValid()
        {
            var mapper = InitializeServices().Mapper;
            mapper.AssertConfigurationIsValid<PersistedGrantMapperProfile>();
        }

        [Fact]
        public void CanMap()
        {
            var model = new PersistedGrant()
            {
                ConsumedTime = new System.DateTime(2020, 02, 03, 4, 5, 6),
            };

            var mapper = InitializeServices().Mapper;

            var mappedEntity = mapper.ToEntity(model);
            mappedEntity.Should().NotBeNull();
            mappedEntity.ConsumedTime.Should().NotBeNull();
            mappedEntity.ConsumedTime.Value.Should().Be(new System.DateTime(2020, 02, 03, 4, 5, 6));

            var mappedModel = mapper.ToModel(mappedEntity);
            mappedModel.ConsumedTime.Value.Should().Be(new System.DateTime(2020, 02, 03, 4, 5, 6));

            Assert.NotNull(mappedModel);
            Assert.NotNull(mappedEntity);
        }

        [Fact]
        public void ShouldMapSameType()
        {
            var id = Guid.NewGuid().ToString();
            var entity1 = new RavenDb.Storage.Entities.PersistedGrant()
            {
                Key = id,
                SubjectId = "test",
            };

            var entity2 = new RavenDb.Storage.Entities.PersistedGrant()
            {
                Key = id,
                SubjectId = "test 22222",
            };

            var mapper = InitializeServices().Mapper;

            entity1.Should().NotBeEquivalentTo(entity2);
            mapper.Map(entity1, entity2);

            entity1.Should().BeEquivalentTo(entity2);
            entity1.Should().NotBeSameAs(entity2, "not the same instance");
        }

        [Fact]
        public void ShouldMapPersistedGrantKeyToEntityId()
        {
            var model = new PersistedGrant()
            {
                Key = "test-name",
            };

            var mapper = InitializeServices().Mapper;

            RavenDb.Storage.Entities.PersistedGrant entity = mapper.ToEntity(model);
            _output.WriteLine(entity.Id);
            entity.Id.Should().NotBeEmpty();
            entity.Id.Should().EndWith("test-name");
            entity.Id.Should().NotStartWith("test-name");
        }
    }
}