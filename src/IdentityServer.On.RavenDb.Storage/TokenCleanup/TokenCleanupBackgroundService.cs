using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Mcrio.IdentityServer.On.RavenDb.Storage.TokenCleanup
{
    /// <summary>
    /// Background service that cleans up the expired grants.
    /// </summary>
    public class TokenCleanupBackgroundService : BackgroundService
    {
        private readonly IOptions<OperationalStoreOptions> _operationalStoreOptions;
        private readonly ILogger<TokenCleanupBackgroundService> _logger;
        private readonly IServiceProvider _serviceProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="TokenCleanupBackgroundService"/> class.
        /// </summary>
        /// <param name="operationalStoreOptions">Options.</param>
        /// <param name="logger">Logger.</param>
        /// <param name="serviceProvider">DI service provider.</param>
        public TokenCleanupBackgroundService(
            IOptions<OperationalStoreOptions> operationalStoreOptions,
            ILogger<TokenCleanupBackgroundService> logger,
            IServiceProvider serviceProvider
        )
        {
            _operationalStoreOptions = operationalStoreOptions;
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        /// <summary>
        /// Executes the background service.
        /// </summary>
        /// <param name="stoppingToken">Cancellation token.</param>
        /// <returns>The <see cref="Task"/> that represents the asynchronous operation.</returns>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogDebug("TokenCleanupBackgroundService service is starting...");

            if (!_operationalStoreOptions.Value.TokenCleanup.EnableTokenCleanupBackgroundService)
            {
                _logger.LogDebug("TokenCleanupBackgroundService is disabled.");
                return;
            }

            try
            {
                if (!stoppingToken.IsCancellationRequested &&
                    _operationalStoreOptions.Value.TokenCleanup.CleanupStartupDelaySec > 0)
                {
                    _logger.LogDebug(
                        "TokenCleanupBackgroundService executing startup delay for {} seconds.",
                        _operationalStoreOptions.Value.TokenCleanup.CleanupStartupDelaySec
                    );
                    await Task.Delay(
                        TimeSpan.FromSeconds(_operationalStoreOptions.Value.TokenCleanup.CleanupStartupDelaySec),
                        stoppingToken
                    );
                }

                while (!stoppingToken.IsCancellationRequested)
                {
                    _logger.LogInformation("TokenCleanupBackgroundService is doing background work.");

                    await CleanupExpiredGrantsAsync(stoppingToken)
                        .ConfigureAwait(false);

                    await Task
                        .Delay(TimeSpan.FromSeconds(_operationalStoreOptions.Value.TokenCleanup.CleanupIntervalSec), stoppingToken)
                        .ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Token cleanup background service exception {}", ex.Message);
            }
            finally
            {
                _logger.LogDebug("TokenCleanupBackgroundService service is stopping.");
            }
        }

        /// <summary>
        /// Cleans up all expired grants.
        /// </summary>
        /// <param name="stoppingToken">Cancellation token.</param>
        /// <returns>The <see cref="Task"/> that represents the asynchronous operation.</returns>
        protected virtual async Task CleanupExpiredGrantsAsync(CancellationToken stoppingToken)
        {
            if (stoppingToken.IsCancellationRequested)
            {
                return;
            }

            try
            {
                using IServiceScope serviceScope = _serviceProvider
                    .GetRequiredService<IServiceScopeFactory>()
                    .CreateScope();

                ITokenCleanupService tokenCleanupService = serviceScope
                    .ServiceProvider
                    .GetRequiredService<ITokenCleanupService>();

                await tokenCleanupService.RemoveExpiredGrantsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cleaning up expired grants. {}", ex.Message);
            }
        }
    }
}