using Raven.Client.Documents.Session;

namespace Mcrio.IdentityServer.On.RavenDb.Storage.RavenDb
{
    /// <summary>
    /// Wrapper for the ravendb document session related to identity.
    /// </summary>
    public interface IIdentityServerDocumentSessionWrapper
    {
        /// <summary>
        /// Gets the RavenDB async document session.
        /// </summary>
        IAsyncDocumentSession Session { get; }
    }
}