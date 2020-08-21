using System;
using Mcrio.IdentityServer.On.RavenDb.Storage;
using Mcrio.IdentityServer.On.RavenDb.Storage.Cors;
using Mcrio.IdentityServer.On.RavenDb.Storage.Stores;
using Mcrio.IdentityServer.On.RavenDb.Storage.TokenCleanup;
using Microsoft.Extensions.DependencyInjection;

namespace Mcrio.IdentityServer.On.RavenDb
{
    /// <summary>
    /// Provides extension methods to configure IdentityServer with RavenDB configuration and operational stores.
    /// </summary>
    public static class IdentityServerRavenDbBuilderExtension
    {
        /// <summary>
        /// Configures IdentityServer with the RavenDB implementation of configuration and operational stores.
        /// </summary>
        /// <returns>Identity server builder.</returns>
        public static IIdentityServerBuilder AddRavenDbStores(
            this IIdentityServerBuilder builder,
            Func<IServiceProvider, IdentityServerDocumentSessionProvider> documentSessionProvider,
            Action<TokenCleanupOptions>? tokenCleanupOptionsAction = null,
            bool addConfigurationStore = true,
            bool addConfigurationStoreCache = true,
            bool addOperationalStore = true)
        {
            builder.Services.IdentityServerAddRavenDbServices(documentSessionProvider);

            if (addConfigurationStore)
            {
                builder.AddClientStore<ClientStore>();
                builder.AddResourceStore<ResourceStore>();
                builder.AddCorsPolicyService<CorsPolicyService>();
                builder.Services.IdentityServerAddConfigurationStoreAdditions();
            }

            if (addConfigurationStoreCache)
            {
                builder.AddInMemoryCaching();

                // add the caching decorators
                builder.AddClientStoreCache<ClientStore>();
                builder.AddResourceStoreCache<ResourceStore>();
                builder.AddCorsPolicyCache<CorsPolicyService>();
            }

            if (addOperationalStore)
            {
                builder.Services.Configure<TokenCleanupOptions>(options =>
                {
                    tokenCleanupOptionsAction?.Invoke(options);
                });

                builder.AddPersistedGrantStore<PersistedGrantStore>();
                builder.AddDeviceFlowStore<DeviceFlowStore>();

                builder.Services.AddTransient<ITokenCleanupService, TokenCleanupService>();
                builder.Services.AddHostedService<TokenCleanupBackgroundService>();
            }

            return builder;
        }
    }
}