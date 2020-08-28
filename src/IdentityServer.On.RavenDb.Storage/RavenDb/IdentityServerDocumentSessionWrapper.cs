using Raven.Client.Documents.Session;

namespace Mcrio.IdentityServer.On.RavenDb.Storage.RavenDb
{
    /// <inheritdoc />
    internal class IdentityServerDocumentSessionWrapper : IIdentityServerDocumentSessionWrapper
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="IdentityServerDocumentSessionWrapper"/> class.
        /// </summary>
        /// <param name="session">RavenDB async document session.</param>
        internal IdentityServerDocumentSessionWrapper(IAsyncDocumentSession session)
        {
            Session = session;
        }

        /// <inheritdoc />
        public IAsyncDocumentSession Session { get; }
    }
}