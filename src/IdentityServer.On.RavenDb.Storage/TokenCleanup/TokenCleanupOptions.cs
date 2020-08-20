namespace Mcrio.IdentityServer.On.RavenDb.Storage.TokenCleanup
{
    /// <summary>
    /// Options for configuring token cleanup.
    /// </summary>
    public class TokenCleanupOptions
    {
        /// <summary>
        /// Gets or sets a value indicating whether stale entries will be automatically cleaned up from the database.
        /// This is implemented by periodically connecting to the database (according to the TokenCleanupInterval) from the hosting application.
        /// Defaults to false.
        /// </summary>
        /// <value>
        ///   <c>true</c> if [enable token cleanup]; otherwise, <c>false</c>.
        /// </value>
        public bool EnableTokenCleanup { get; set; } = false;

        /// <summary>
        /// Gets or sets the token cleanup interval (in seconds). The default is 3600 (1 hour).
        /// </summary>
        /// <value>
        /// The token cleanup interval.
        /// </value>
        public int TokenCleanupIntervalSec { get; set; } = 3600;

        /// <summary>
        /// After token cleanup service starts, how long to wait until the first execution.
        /// </summary>
        public int TokenCleanupStartupDelaySec { get; set; } = 30;

        /// <summary>
        /// Gets or sets the maximum operations per second the RavenDB operation should be allowed to to during
        /// the delete by query operation.
        /// </summary>
        public int? DeleteByQueryMaxOperationsPerSecond { get; set; } = 1024;
    }
}