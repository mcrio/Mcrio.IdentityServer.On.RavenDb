using System.Linq;
using System.Threading.Tasks;
using IdentityServer4.Services;
using Mcrio.IdentityServer.On.RavenDb.Storage.Entities;
using Microsoft.Extensions.Logging;
using Raven.Client.Documents;
using Raven.Client.Documents.Linq;
using Raven.Client.Documents.Session;

namespace Mcrio.IdentityServer.On.RavenDb.Storage.Cors
{
    public class CorsPolicyService : ICorsPolicyService
    {
        private readonly IAsyncDocumentSession _documentSession;
        private readonly ILogger<CorsPolicyService> _logger;

        public CorsPolicyService(
            DocumentSessionProvider documentSessionProvider,
            ILogger<CorsPolicyService> logger)
        {
            /*
             * NOTE: In case we would be injecting the scoped document session service directly we would
             * need to pull it through the IHttpContextAccessor because of https://github.com/aspnet/CORS/issues/105
             * (per official EF core implementation comment)
             */
            _documentSession = documentSessionProvider();
            _logger = logger;
        }

        /// <inheritdoc/>
        public async Task<bool> IsOriginAllowedAsync(string origin)
        {
            origin = origin.ToLowerInvariant();

            bool isAllowed = await _documentSession
                .Query<Client>()
                .Where(client => client.AllowedCorsOrigins.Any(item => item == origin))
                .AnyAsync()
                .ConfigureAwait(false);

            _logger.LogDebug("Origin {origin} is allowed: {originAllowed}", origin, isAllowed);

            return isAllowed;
        }
    }
}