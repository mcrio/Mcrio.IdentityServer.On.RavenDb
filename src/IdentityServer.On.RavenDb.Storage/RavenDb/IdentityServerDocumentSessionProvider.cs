using Raven.Client.Documents;
using Raven.Client.Documents.Session;

namespace Mcrio.IdentityServer.On.RavenDb.Storage.RavenDb
{
    /// <summary>
    /// Provides the RavenDB document session related to identity server.
    /// </summary>
    /// <returns>Instance of RavenDb async document session.</returns>
    public delegate IAsyncDocumentSession IdentityServerDocumentSessionProvider();

    /// <summary>
    /// Provides the RavenDb document store related to identity server.
    /// </summary>
    /// <returns>Instance of the <see cref="IDocumentStore"/>.</returns>
    public delegate IDocumentStore IdentityServerDocumentStoreProvider();
}