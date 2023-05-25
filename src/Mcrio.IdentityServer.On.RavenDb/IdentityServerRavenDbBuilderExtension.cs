using System;
using IdentityServer4.Models;
using Mcrio.IdentityServer.On.RavenDb.Storage;
using Mcrio.IdentityServer.On.RavenDb.Storage.Cors;
using Mcrio.IdentityServer.On.RavenDb.Storage.Stores;
using Mcrio.IdentityServer.On.RavenDb.Storage.Stores.Utility;
using Mcrio.IdentityServer.On.RavenDb.Storage.TokenCleanup;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

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
            IdentityServerDocumentSessionServiceLocator documentSessionServiceLocator,
            IdentityServerDocumentStoreServiceLocator documentStoreServiceLocator,
            Action<OperationalStoreOptions>? operationalStoreOptions = null,
            bool addConfigurationStore = true,
            bool addConfigurationStoreCache = true,
            bool addOperationalStore = true)
        {
            return AddRavenDbStores<Storage.Entities.PersistedGrant, PersistedGrantStore,
                Storage.Entities.DeviceFlowCode, DeviceFlowStore, UniqueReservation>(
                builder,
                documentSessionServiceLocator,
                documentStoreServiceLocator,
                operationalStoreOptions,
                addConfigurationStore,
                addConfigurationStoreCache,
                addOperationalStore
            );
        }

        /// <summary>
        /// Configures IdentityServer with the RavenDB implementation of configuration and operational stores.
        /// </summary>
        /// <returns>Identity server builder.</returns>
        public static IIdentityServerBuilder AddRavenDbStores<TPersistedGrantEntity, TPersistedGrantStore,
            TDeviceFlowCode, TDeviceFlowStore, TUniqueReservation>(
            this IIdentityServerBuilder builder,
            IdentityServerDocumentSessionServiceLocator documentSessionServiceLocator,
            IdentityServerDocumentStoreServiceLocator documentStoreServiceLocator,
            Action<OperationalStoreOptions>? operationalStoreOptions = null,
            bool addConfigurationStore = true,
            bool addConfigurationStoreCache = true,
            bool addOperationalStore = true)
            where TPersistedGrantEntity : Storage.Entities.PersistedGrant
            where TDeviceFlowCode : Storage.Entities.DeviceFlowCode, new()
            where TPersistedGrantStore : PersistedGrantStore<TPersistedGrantEntity>
            where TDeviceFlowStore : DeviceFlowStore<TDeviceFlowCode, TUniqueReservation>
            where TUniqueReservation : UniqueReservation
        {
            return AddRavenDbStores<ClientStore, ResourceStore, CorsPolicyService,
                ClientStoreExtension, ResourceStoreExtension, TPersistedGrantStore, TDeviceFlowStore,
                TokenCleanupService, TokenCleanupBackgroundService, Client, Storage.Entities.Client,
                IdentityResource, Storage.Entities.IdentityResource, ApiResource, Storage.Entities.ApiResource,
                ApiScope, Storage.Entities.ApiScope, TPersistedGrantEntity, TDeviceFlowCode, TUniqueReservation>(
                builder,
                documentSessionServiceLocator,
                documentStoreServiceLocator,
                operationalStoreOptions,
                addConfigurationStore,
                addConfigurationStoreCache,
                addOperationalStore
            );
        }

        /// <summary>
        /// Configures IdentityServer with the RavenDB implementation of configuration and operational stores.
        /// </summary>
        /// <returns>Identity server builder.</returns>
        public static IIdentityServerBuilder AddRavenDbStores<TClientStore, TResourceStore, TCorsPolicyService,
            TClientStoreExtension, TResourceStoreExtension, TPersistedGrantStore, TDeviceFlowStore,
            TTokenCleanupService, TTokenCleanupBackgroundService, TClient, TClientEntity,
            TIdentityResource, TIdentityResourceEntity, TApiResource, TApiResourceEntity,
            TApiScope, TApiScopeEntity, TPersistedGrantEntity, TDeviceFlowCode, TUniqueReservation>(
            this IIdentityServerBuilder builder,
            IdentityServerDocumentSessionServiceLocator documentSessionServiceLocator,
            IdentityServerDocumentStoreServiceLocator documentStoreServiceLocator,
            Action<OperationalStoreOptions>? operationalStoreOptions = null,
            bool addConfigurationStore = true,
            bool addConfigurationStoreCache = true,
            bool addOperationalStore = true)
            where TClientStore : ClientStore<TClientEntity>
            where TResourceStore : ResourceStore<TIdentityResourceEntity, TApiResourceEntity, TApiScopeEntity>
            where TCorsPolicyService : CorsPolicyService<TClientEntity>
            where TClientStoreExtension : ClientStoreExtension<TClient, TClientEntity>
            where TResourceStoreExtension : ResourceStoreExtension<TIdentityResource, TIdentityResourceEntity,
                TApiResource, TApiResourceEntity, TApiScope, TApiScopeEntity>
            where TPersistedGrantStore : PersistedGrantStore<TPersistedGrantEntity>
            where TDeviceFlowStore : DeviceFlowStore<TDeviceFlowCode, TUniqueReservation>
            where TTokenCleanupService : TokenCleanupService
            where TTokenCleanupBackgroundService : TokenCleanupBackgroundService
            where TClient : Client
            where TClientEntity : Storage.Entities.Client
            where TIdentityResource : IdentityResource
            where TIdentityResourceEntity : Storage.Entities.IdentityResource
            where TApiResource : ApiResource
            where TApiResourceEntity : Storage.Entities.ApiResource
            where TApiScope : ApiScope
            where TApiScopeEntity : Storage.Entities.ApiScope
            where TPersistedGrantEntity : Storage.Entities.PersistedGrant
            where TDeviceFlowCode : Storage.Entities.DeviceFlowCode, new()
            where TUniqueReservation : UniqueReservation
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (documentSessionServiceLocator == null)
            {
                throw new ArgumentNullException(nameof(documentSessionServiceLocator));
            }

            builder.Services.IdentityServerAddRavenDbServices(
                documentSessionServiceLocator,
                documentStoreServiceLocator
            );

            if (addConfigurationStore)
            {
                builder.AddClientStore<TClientStore>();
                builder.AddResourceStore<TResourceStore>();
                builder.AddCorsPolicyService<TCorsPolicyService>();

                builder.Services.TryAddTransient<IClientStoreExtension<TClient>, TClientStoreExtension>();
                builder.Services.TryAddTransient<
                    IResourceStoreExtension<TIdentityResource, TApiResource, TApiScope>,
                    TResourceStoreExtension
                >();
            }

            if (addConfigurationStoreCache)
            {
                builder.AddInMemoryCaching();

                // add the caching decorators
                builder.AddClientStoreCache<TClientStore>();
                builder.AddResourceStoreCache<TResourceStore>();
                builder.AddCorsPolicyCache<TCorsPolicyService>();
            }

            if (addOperationalStore)
            {
                builder.Services.Configure<OperationalStoreOptions>(options =>
                {
                    operationalStoreOptions?.Invoke(options);
                });

                builder.AddPersistedGrantStore<TPersistedGrantStore>();
                builder.AddDeviceFlowStore<TDeviceFlowStore>();

                builder.Services.AddTransient<ITokenCleanupService, TTokenCleanupService>();
                builder.Services.AddHostedService<TTokenCleanupBackgroundService>();
            }

            return builder;
        }
    }
}