using System;
using System.Threading.Tasks;
using FluentAssertions;
using IdentityServer4.Services;
using IdentityServer4.Stores;
using IdentityServer4.Stores.Serialization;
using Mcrio.IdentityServer.On.RavenDb.Storage.Cors;
using Mcrio.IdentityServer.On.RavenDb.Storage.Mappers;
using Mcrio.IdentityServer.On.RavenDb.Storage.Stores;
using Mcrio.IdentityServer.On.RavenDb.Storage.Stores.Advanced;
using Mcrio.IdentityServer.On.RavenDb.Storage.TokenCleanup;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Raven.Client.Documents;
using Raven.Client.Documents.Conventions;
using Raven.Client.Documents.Operations.CompareExchange;
using Raven.Client.Documents.Session;
using Raven.TestDriver;

namespace Mcrio.IdentityServer.On.RavenDb.Storage.Tests.IntegrationTests
{
    public abstract class IntegrationTestBase : RavenTestDriver
    {
        private IDocumentStore? _documentStore;

        protected override void PreInitialize(IDocumentStore documentStore)
        {
            documentStore.Conventions.FindCollectionName = type =>
            {
                return RavenDbConventions.GetIdentityServerCollectionName(type)
                       ?? DocumentConventions.DefaultGetCollectionName(type);
            };
        }

        protected ServiceScope InitializeServices(
            Action<TokenCleanupOptions>? tokenCleanupOptionsAction = null)
        {
            _documentStore ??= GetDocumentStore();

            var serviceCollection = new ServiceCollection();

            serviceCollection.TryAddSingleton(provider =>
                _documentStore.OpenAsyncSession()
            );
            serviceCollection.TryAddScoped(_ => _documentStore.OpenAsyncSession());

            serviceCollection.AddHttpContextAccessor();
            serviceCollection.AddLogging();
            serviceCollection.AddDataProtection();

            // Identity server ravendb related services
            serviceCollection.IdentityServerAddRavenDbServices(
                provider => provider.GetRequiredService<IAsyncDocumentSession>
            );

            // Identity Server operational services
            serviceCollection.Configure<TokenCleanupOptions>(
                cleanUpOptions => tokenCleanupOptionsAction?.Invoke(cleanUpOptions)
            );
            serviceCollection.AddTransient<IPersistedGrantStore, PersistedGrantStore>();
            serviceCollection.AddTransient<IDeviceFlowStore, DeviceFlowStore>();
            serviceCollection.AddTransient<ITokenCleanupService, TokenCleanupService>();
            serviceCollection.AddHostedService<TokenCleanupBackgroundService>();

            // Identity Server configuration services
            serviceCollection.TryAddScoped<IClientStore, ClientStore>();
            serviceCollection.TryAddScoped<IResourceStore, ResourceStore>();
            serviceCollection.TryAddScoped<ICorsPolicyService, CorsPolicyService>();
            serviceCollection.IdentityServerAddConfigurationStoreAdditions();

            // Identity server other services required for tests
            serviceCollection.TryAddSingleton<IPersistentGrantSerializer, PersistentGrantSerializer>();

            ServiceProvider serviceProvider = serviceCollection.BuildServiceProvider();

            return new ServiceScope(
                _documentStore,
                serviceProvider.GetRequiredService<IClientStore>(),
                serviceProvider.GetRequiredService<IResourceStore>(),
                serviceProvider.GetRequiredService<IDeviceFlowStore>(),
                serviceProvider.GetRequiredService<IPersistedGrantStore>(),
                serviceProvider.GetRequiredService<IIdentityServerStoreMapper>(),
                serviceProvider.GetRequiredService<IAsyncDocumentSession>(),
                serviceProvider.GetRequiredService<ICorsPolicyService>(),
                serviceProvider.GetRequiredService<ITokenCleanupService>(),
                serviceProvider.GetRequiredService<IClientStoreAdditions>(),
                serviceProvider.GetRequiredService<IResourceStoreAdditions>()
            );
        }

        protected async Task AssertCompareExchangeKeyExistsAsync(string cmpExchangeKey, string because = "")
        {
            IDocumentStore documentStore = InitializeServices().DocumentStore;
            CompareExchangeValue<string> result = await GetCompareExchangeAsync<string>(documentStore, cmpExchangeKey);
            result.Should().NotBeNull(
                $"cmp exchange {cmpExchangeKey} should exist because {because}"
            );
        }

        protected async Task AssertCompareExchangeKeyExistsWithValueAsync<TValue>(
            string cmpExchangeKey,
            TValue value,
            string because = "")
        {
            IDocumentStore documentStore = InitializeServices().DocumentStore;
            CompareExchangeValue<TValue> result = await GetCompareExchangeAsync<TValue>(documentStore, cmpExchangeKey);
            result.Should().NotBeNull(
                $"cmp exchange {cmpExchangeKey} should exist because {because}"
            );
            result.Value.Should().Be(value);
        }

        protected async Task AssertCompareExchangeKeyDoesNotExistAsync(string cmpExchangeKey, string because = "")
        {
            IDocumentStore documentStore = InitializeServices().DocumentStore;
            CompareExchangeValue<string> result = await GetCompareExchangeAsync<string>(documentStore, cmpExchangeKey);
            result.Should().BeNull(
                $"cmp exchange {cmpExchangeKey} should not exist because {because}"
            );
        }

        private static Task<CompareExchangeValue<TValue>> GetCompareExchangeAsync<TValue>(
            IDocumentStore documentStore,
            string cmpExchangeKey)
        {
            return documentStore.Operations.SendAsync(
                new GetCompareExchangeValueOperation<TValue>(cmpExchangeKey)
            );
        }
    }
}