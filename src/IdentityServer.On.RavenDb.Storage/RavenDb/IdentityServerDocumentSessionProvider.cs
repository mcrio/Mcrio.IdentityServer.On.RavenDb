using Raven.Client.Documents.Session;

namespace Mcrio.IdentityServer.On.RavenDb.Storage.RavenDb
{
    /// <summary>
    /// Provides the RavenDB document session.
    /// </summary>
    /// <returns>Instance of RavenDb async document session.</returns>
    public delegate IAsyncDocumentSession IdentityServerDocumentSessionProvider();
}