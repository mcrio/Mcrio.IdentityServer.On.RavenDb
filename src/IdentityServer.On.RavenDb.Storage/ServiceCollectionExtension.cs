using System;
using Mcrio.IdentityServer.On.RavenDb.Storage.Mappers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Raven.Client.Documents.Session;

namespace Mcrio.IdentityServer.On.RavenDb.Storage
{
    /// <summary>
    /// Delegate which provides a <see cref="IAsyncDocumentSession"/> to be used as a RavenDB document session.
    /// </summary>
    /// <returns>RavenDB async document session.</returns>
    public delegate IAsyncDocumentSession IdentityServerDocumentSessionProvider();

    /// <summary>
    /// Provides extension methods for registering required services to the DI container.
    /// </summary>
    public static class IdentityServerRavenDbServiceCollectionExtension
    {
        public static IServiceCollection IdentityServerAddRavenDbServices(
            this IServiceCollection serviceCollection,
            Func<IServiceProvider, IdentityServerDocumentSessionProvider> documentSessionProvider)
        {
            if (documentSessionProvider == null)
            {
                throw new ArgumentNullException(nameof(documentSessionProvider));
            }

            serviceCollection.TryAddScoped(documentSessionProvider);
            serviceCollection.TryAddScoped<IIdentityServerStoreMapper, IdentityServerStoreMapper>();

            return serviceCollection;
        }
    }
}