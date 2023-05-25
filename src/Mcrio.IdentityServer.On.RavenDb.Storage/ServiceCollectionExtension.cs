using System;
using Mcrio.IdentityServer.On.RavenDb.Storage.Mappers;
using Mcrio.IdentityServer.On.RavenDb.Storage.RavenDb;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Raven.Client.Documents;
using Raven.Client.Documents.Session;

namespace Mcrio.IdentityServer.On.RavenDb.Storage
{
    /// <summary>
    /// Locates the RavenDb document session service.
    /// </summary>
    /// <param name="serviceProvider">Service provider.</param>
    /// <returns>Instance of <see cref="IAsyncDocumentSession"/>.</returns>
    public delegate IAsyncDocumentSession IdentityServerDocumentSessionServiceLocator(IServiceProvider serviceProvider);

    /// <summary>
    /// Locates the RavenDb document store service.
    /// </summary>
    /// <param name="serviceProvider">Service provider.</param>
    /// <returns>Instance of <see cref="IDocumentStore"/>.</returns>
    public delegate IDocumentStore IdentityServerDocumentStoreServiceLocator(IServiceProvider serviceProvider);

    /// <summary>
    /// Provides extension methods for registering required services to the DI container.
    /// </summary>
    public static class IdentityServerRavenDbServiceCollectionExtension
    {
        /// <summary>
        /// Register services.
        /// </summary>
        /// <param name="serviceCollection">Service collection.</param>
        /// <param name="documentSessionServiceLocator">RavenDb document session service locator.</param>
        /// <param name="documentStoreServiceLocator">RavenDb document store. </param>
        /// <param name="uniqueValuesReservationOptionsConfig">Configure Unique value reservations options.</param>
        /// <returns>Same service collection the extension method is applied on.</returns>
        public static IServiceCollection IdentityServerAddRavenDbServices(
            this IServiceCollection serviceCollection,
            IdentityServerDocumentSessionServiceLocator documentSessionServiceLocator,
            IdentityServerDocumentStoreServiceLocator documentStoreServiceLocator,
            Action<UniqueValuesReservationOptions>? uniqueValuesReservationOptionsConfig = null)
        {
            if (documentSessionServiceLocator == null)
            {
                throw new ArgumentNullException(nameof(documentSessionServiceLocator));
            }

            var uniqueValueRelatedOptions = new UniqueValuesReservationOptions();
            uniqueValuesReservationOptionsConfig?.Invoke(uniqueValueRelatedOptions);
            serviceCollection.TryAddSingleton(uniqueValueRelatedOptions);

            // Identity server related Ravendb document session provider
            serviceCollection.TryAddScoped<IdentityServerDocumentSessionProvider>(
                provider => () => documentSessionServiceLocator(provider)
            );

            // Identity server related Ravendb document store provider
            serviceCollection.TryAddScoped<IdentityServerDocumentStoreProvider>(
                provider => () => documentStoreServiceLocator(provider)
            );

            // Register singleton mapper
            serviceCollection.TryAddSingleton<IIdentityServerStoreMapper, IdentityServerStoreMapper>();

            return serviceCollection;
        }
    }
}