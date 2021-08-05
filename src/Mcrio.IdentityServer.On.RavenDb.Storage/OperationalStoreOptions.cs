namespace Mcrio.IdentityServer.On.RavenDb.Storage
{
    /// <summary>
    /// Options for configuring operational stores.
    /// </summary>
    public class OperationalStoreOptions
    {
        /// <summary>
        /// Gets or sets a value indicating whether the @expires metadata should be set on the persisted grants.
        /// That way tokens cleanup operation can be offloaded to RavenDb using its Document Expiration feature.
        /// This feature needs to be turned on on database level. Clean interval is set at db level too.
        /// Read more at:
        /// https://ravendb.net/docs/article-page/5.0/csharp/studio/database/settings/document-expiration .
        /// </summary>
        public bool SetRavenDbDocumentExpiresMetadata { get; set; } = true;

        /// <summary>
        /// Token cleanup options.
        /// </summary>
        public TokenCleanupOptions TokenCleanup { get; set; } = new TokenCleanupOptions();

        /// <summary>
        /// Token cleanup options.
        /// </summary>
        public class TokenCleanupOptions
        {
            /// <summary>
            /// Gets or sets a value indicating whether stale entries will be automatically cleaned up from the database.
            /// This is implemented by periodically connecting to the database (according to the TokenCleanupInterval) from the hosting application.
            ///
            /// Defaults to false.
            ///
            /// If <see cref="SetRavenDbDocumentExpiresMetadata"/> is set to true and RavenDB internal cleanup process is
            /// enabled, then we can disable the background service as RavenDb will take care of cleaning up expired documents.
            /// </summary>
            /// <value>
            ///   <c>true</c> if [enable token cleanup]; otherwise, <c>false</c>.
            /// </value>
            public bool EnableTokenCleanupBackgroundService { get; set; } = false;

            /// <summary>
            /// Gets or sets the token cleanup interval (in seconds). The default is 3600 (1 hour).
            /// </summary>
            /// <value>
            /// The token cleanup interval.
            /// </value>
            public int CleanupIntervalSec { get; set; } = 3600;

            /// <summary>
            /// After token cleanup service starts, how long to wait until the first execution.
            /// </summary>
            public int CleanupStartupDelaySec { get; set; } = 30;

            /// <summary>
            /// Gets or sets the maximum operations per second the RavenDB operation should be allowed to do during
            /// the delete by query operation.
            /// </summary>
            public int? DeleteByQueryMaxOperationsPerSecond { get; set; } = 1024;
        }
    }
}