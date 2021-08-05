using Mcrio.IdentityServer.On.RavenDb.Storage.Mappers;
using Xunit;

namespace Mcrio.IdentityServer.On.RavenDb.Storage.Tests.IntegrationTests.Mappings
{
    public class MapperTests : IntegrationTestBase
    {
        [Fact]
        public void ScopeAutomapperConfigurationIsValid()
        {
            IIdentityServerStoreMapper mapper = InitializeServices().Mapper;
            mapper.AssertConfigurationIsValid();
        }
    }
}