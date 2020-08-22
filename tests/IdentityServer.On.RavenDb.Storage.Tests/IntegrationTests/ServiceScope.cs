using IdentityServer4.Models;
using IdentityServer4.Services;
using IdentityServer4.Stores;
using Mcrio.IdentityServer.On.RavenDb.Storage.Mappers;
using Mcrio.IdentityServer.On.RavenDb.Storage.Stores.Additions;
using Mcrio.IdentityServer.On.RavenDb.Storage.TokenCleanup;
using Raven.Client.Documents;
using Raven.Client.Documents.Session;

namespace Mcrio.IdentityServer.On.RavenDb.Storage.Tests.IntegrationTests
{
    public class ServiceScope
    {
        internal ServiceScope(
            IDocumentStore documentStore,
            IClientStore clientStore,
            IResourceStore resourceStore,
            IDeviceFlowStore deviceFlowStore,
            IPersistedGrantStore persistedGrantStore,
            IIdentityServerStoreMapper mapper,
            IAsyncDocumentSession documentSession,
            ICorsPolicyService corsPolicyService,
            ITokenCleanupService tokenCleanupService,
            IClientStoreAdditions<Client> clientStoreAdditions,
            IResourceStoreAdditions<IdentityResource, ApiResource, ApiScope> resourceStoreAdditions)
        {
            ClientStore = clientStore;
            ResourceStore = resourceStore;
            DeviceFlowStore = deviceFlowStore;
            PersistedGrantStore = persistedGrantStore;
            Mapper = mapper;
            DocumentSession = documentSession;
            CorsPolicyService = corsPolicyService;
            TokenCleanupService = tokenCleanupService;
            ClientStoreAdditions = clientStoreAdditions;
            ResourceStoreAdditions = resourceStoreAdditions;
            DocumentStore = documentStore;
        }

        internal IDocumentStore DocumentStore { get; }

        internal IClientStore ClientStore { get; }

        internal IClientStoreAdditions<Client> ClientStoreAdditions { get; }

        internal IResourceStore ResourceStore { get; }

        internal IResourceStoreAdditions<IdentityResource, ApiResource, ApiScope> ResourceStoreAdditions { get; }

        internal IDeviceFlowStore DeviceFlowStore { get; }

        internal IPersistedGrantStore PersistedGrantStore { get; }

        internal IIdentityServerStoreMapper Mapper { get; }

        internal IAsyncDocumentSession DocumentSession { get; }

        internal ICorsPolicyService CorsPolicyService { get; }

        internal ITokenCleanupService TokenCleanupService { get; }
    }
}