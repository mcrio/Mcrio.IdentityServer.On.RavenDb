using System;
using System.Threading.Tasks;
using Mcrio.IdentityServer.On.RavenDb.Storage.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Raven.Client.Documents;
using Raven.Client.Documents.Linq;
using Raven.Client.Documents.Operations;
using Raven.Client.Documents.Queries;
using Raven.Client.Documents.Session;

namespace Mcrio.IdentityServer.On.RavenDb.Storage.TokenCleanup
{
    /// <summary>
    /// Helper to periodically cleanup expired persisted grants.
    /// </summary>
    public class TokenCleanupService : ITokenCleanupService
    {
        private readonly IAsyncDocumentSession _documentSession;
        private readonly ILogger<TokenCleanupService> _logger;
        private readonly IOptionsSnapshot<TokenCleanupOptions> _tokenCleanupOptions;

        /// <summary>
        /// Initializes a new instance of the <see cref="TokenCleanupService"/> class.
        /// </summary>
        /// <param name="documentSessionProvider">Provider the document session.</param>
        /// <param name="tokenCleanupOptions">Options.</param>
        /// <param name="logger">Logger.</param>
        public TokenCleanupService(
            DocumentSessionProvider documentSessionProvider,
            IOptionsSnapshot<TokenCleanupOptions> tokenCleanupOptions,
            ILogger<TokenCleanupService> logger)
        {
            _documentSession = documentSessionProvider();
            _logger = logger;
            _tokenCleanupOptions = tokenCleanupOptions;
        }

        /// <inheritdoc/>
        public virtual async Task RemoveExpiredGrantsAsync()
        {
            try
            {
                await RemoveStalePersistedGrantsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError("TokenCleanupService exception removing expired grants: {exception}", ex.Message);
            }

            try
            {
                await RemoveExpiredDeviceCodesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError("TokenCleanupService exception removing expired device codes: {exception}", ex.Message);
            }
        }

        /// <summary>
        /// Remove expired device flow codes.
        /// </summary>
        /// <returns>The <see cref="Task"/> that represents the asynchronous operation.</returns>
        protected virtual Task RemoveExpiredDeviceCodesAsync()
        {
            /*
             * Note: For performance reasons we won't implement the IOperationalStoreNotification that is
             * done on the EF core side. Purpose of it was to make a notification about every single deletion.
             */
            IRavenQueryable<DeviceFlowCode> query = _documentSession
                .Query<DeviceFlowCode>()
                .Where(deviceFlowCode => deviceFlowCode.Expiration < DateTime.UtcNow);
            return PerformDeleteOperation(query);
        }

        /// <summary>
        /// Removes stale persisted grants.
        /// </summary>
        /// <returns>The <see cref="Task"/> that represents the asynchronous operation.</returns>
        protected virtual Task RemoveStalePersistedGrantsAsync()
        {
            /*
             * Note: For performance reasons we won't implement the IOperationalStoreNotification that is
             * done on the EF core side. Purpose of it was to make a notification about every single deletion.
             */
            IRavenQueryable<PersistedGrant> query = _documentSession
                .Query<PersistedGrant>()
                .Where(grant => grant.Expiration < DateTime.UtcNow);
            return PerformDeleteOperation(query);
        }

        private async Task PerformDeleteOperation<TEntity>(IRavenQueryable<TEntity> query)
        {
            const int deleteOperationTimeoutSec = 30;
            try
            {
                int? deleteByQueryMaxOperations = _tokenCleanupOptions.Value.DeleteByQueryMaxOperationsPerSecond;
                Operation operation = await _documentSession.Advanced.DocumentStore.Operations.SendAsync(
                    new DeleteByQueryOperation(
                        query.ToAsyncDocumentQuery().GetIndexQuery(),
                        new QueryOperationOptions
                        {
                            MaxOpsPerSecond = deleteByQueryMaxOperations,
                        })
                ).ConfigureAwait(false);
                await operation
                    .WaitForCompletionAsync(TimeSpan.FromSeconds(deleteOperationTimeoutSec))
                    .ConfigureAwait(false);
            }
            catch (TimeoutException)
            {
                _logger.LogWarning(
                    "TokenCleanupService remove expired grants of type {} operation took more than {} seconds.",
                    typeof(TEntity).ToString(),
                    deleteOperationTimeoutSec
                );
            }
        }
    }
}