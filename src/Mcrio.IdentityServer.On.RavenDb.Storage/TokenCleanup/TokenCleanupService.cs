using System;
using System.Threading.Tasks;
using Mcrio.IdentityServer.On.RavenDb.Storage.Entities;
using Mcrio.IdentityServer.On.RavenDb.Storage.Extensions;
using Mcrio.IdentityServer.On.RavenDb.Storage.RavenDb;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Raven.Client;
using Raven.Client.Documents;
using Raven.Client.Documents.Operations;
using Raven.Client.Documents.Queries;

namespace Mcrio.IdentityServer.On.RavenDb.Storage.TokenCleanup
{
    /// <summary>
    /// Helper to periodically cleanup expired persisted grants.
    /// </summary>
    public class TokenCleanupService : ITokenCleanupService
    {
        private readonly IDocumentStore _documentStore;
        private readonly ILogger<TokenCleanupService> _logger;
        private readonly IOptionsSnapshot<OperationalStoreOptions> _operationalStoreOptions;

        /// <summary>
        /// Initializes a new instance of the <see cref="TokenCleanupService"/> class.
        /// </summary>
        /// <param name="identityServerDocumentStoreProvider">Document store provider.</param>
        /// <param name="operationalStoreOptions">Options.</param>
        /// <param name="logger">Logger.</param>
        public TokenCleanupService(
            IdentityServerDocumentStoreProvider identityServerDocumentStoreProvider,
            IOptionsSnapshot<OperationalStoreOptions> operationalStoreOptions,
            ILogger<TokenCleanupService> logger)
        {
            _documentStore = identityServerDocumentStoreProvider();
            _logger = logger;
            _operationalStoreOptions = operationalStoreOptions;
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
                _logger.LogError(
                    "TokenCleanupService exception removing expired device codes: {exception}",
                    ex.Message
                );
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
            var query = new IndexQuery
            {
                Query =
                    $"from {_documentStore.GetCollectionName(typeof(DeviceFlowCode))} where {nameof(DeviceFlowCode.Expiration)} < $utcNow",
                QueryParameters = new Parameters
                {
                    { "utcNow", DateTime.UtcNow },
                },
            };
            return PerformDeleteOperation(query, typeof(DeviceFlowCode));
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
            var query = new IndexQuery
            {
                Query =
                    $"from {_documentStore.GetCollectionName(typeof(PersistedGrant))} where {nameof(PersistedGrant.Expiration)} < $utcNow",
                QueryParameters = new Parameters
                {
                    { "utcNow", DateTime.UtcNow },
                },
            };
            return PerformDeleteOperation(query, typeof(PersistedGrant));
        }

        private async Task PerformDeleteOperation(IndexQuery query, Type entityType)
        {
            const int deleteOperationTimeoutSec = 30;
            try
            {
                int? deleteByQueryMaxOperations = _operationalStoreOptions
                    .Value
                    .TokenCleanup
                    .DeleteByQueryMaxOperationsPerSecond;

                Operation operation = await _documentStore.Operations.SendAsync(
                    new DeleteByQueryOperation(
                        query,
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
                    entityType.ToString(),
                    deleteOperationTimeoutSec
                );
            }
        }
    }
}