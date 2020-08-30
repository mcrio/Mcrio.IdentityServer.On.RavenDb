using System;
using Mcrio.IdentityServer.On.RavenDb.Storage.Mappers;
using Mcrio.IdentityServer.On.RavenDb.Storage.RavenDb;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Raven.Client.Documents.Session;

namespace Mcrio.IdentityServer.On.RavenDb.Storage
{
    /// <summary>
    /// Provides extension methods for registering required services to the DI container.
    /// </summary>
    public static class IdentityServerRavenDbServiceCollectionExtension
    {
        /// <summary>
        /// Register services.
        /// </summary>
        /// <param name="serviceCollection">Service collection.</param>
        /// <param name="documentSessionProvider">RavenDb document session provider.</param>
        /// <returns>Same service collection the extension method is applied on.</returns>
        public static IServiceCollection IdentityServerAddRavenDbServices(
            this IServiceCollection serviceCollection,
            Func<IServiceProvider, IAsyncDocumentSession> documentSessionProvider)
        {
            if (documentSessionProvider == null)
            {
                throw new ArgumentNullException(nameof(documentSessionProvider));
            }

            // Identity server related Ravendb document session wrapper
            serviceCollection.TryAddScoped<IIdentityServerDocumentSessionWrapper>(
                provider => new IdentityServerDocumentSessionWrapper(documentSessionProvider(provider))
            );

            // Register singleton mapper
            serviceCollection.TryAddSingleton<IIdentityServerStoreMapper, IdentityServerStoreMapper>();

            return serviceCollection;
        }
    }
}