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
        private readonly IOptions<TokenCleanupOptions> _tokenCleanupOptions;
        private readonly ILogger<TokenCleanupBackgroundService> _logger;
        private readonly IServiceProvider _serviceProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="TokenCleanupBackgroundService"/> class.
        /// </summary>
        /// <param name="tokenCleanupOptions">Options.</param>
        /// <param name="logger">Logger.</param>
        /// <param name="serviceProvider">DI service provider.</param>
        public TokenCleanupBackgroundService(
            IOptions<TokenCleanupOptions> tokenCleanupOptions,
            ILogger<TokenCleanupBackgroundService> logger,
            IServiceProvider serviceProvider
        )
        {
            _tokenCleanupOptions = tokenCleanupOptions;
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

            if (!_tokenCleanupOptions.Value.EnableTokenCleanup)
            {
                _logger.LogDebug("TokenCleanupBackgroundService is disabled.");
                return;
            }

            try
            {
                if (!stoppingToken.IsCancellationRequested &&
                    _tokenCleanupOptions.Value.TokenCleanupStartupDelaySec > 0)
                {
                    _logger.LogDebug(
                        "TokenCleanupBackgroundService executing startup delay for {} seconds.",
                        _tokenCleanupOptions.Value.TokenCleanupStartupDelaySec
                    );
                    await Task.Delay(
                        TimeSpan.FromSeconds(_tokenCleanupOptions.Value.TokenCleanupStartupDelaySec),
                        stoppingToken
                    );
                }

                while (!stoppingToken.IsCancellationRequested)
                {
                    _logger.LogDebug("TokenCleanupBackgroundService is doing background work.");

                    await CleanupExpiredGrantsAsync(stoppingToken)
                        .ConfigureAwait(false);

                    await Task
                        .Delay(TimeSpan.FromSeconds(_tokenCleanupOptions.Value.TokenCleanupIntervalSec), stoppingToken)
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