using System.Linq;
using System.Threading.Tasks;
using IdentityServer4.Services;
using Microsoft.Extensions.Logging;
using Raven.Client.Documents;
using Raven.Client.Documents.Linq;
using Raven.Client.Documents.Session;

namespace Mcrio.IdentityServer.On.RavenDb.Storage.Cors
{
    public class CorsPolicyService : CorsPolicyService<Entities.Client>
    {
        public CorsPolicyService(
            IdentityServerDocumentSessionProvider identityServerDocumentSessionProvider,
            ILogger<CorsPolicyService> logger)
            : base(identityServerDocumentSessionProvider, logger)
        {
        }
    }

    public abstract class CorsPolicyService<TClientEntity> : ICorsPolicyService
        where TClientEntity : Entities.Client
    {
        private readonly IAsyncDocumentSession _documentSession;
        private readonly ILogger<CorsPolicyService<TClientEntity>> _logger;

        public CorsPolicyService(
            IdentityServerDocumentSessionProvider identityServerDocumentSessionProvider,
            ILogger<CorsPolicyService<TClientEntity>> logger)
        {
            /*
             * NOTE: In case we would be injecting the scoped document session service directly we would
             * need to pull it through the IHttpContextAccessor because of https://github.com/aspnet/CORS/issues/105
             * (per official EF core implementation comment)
             */
            _documentSession = identityServerDocumentSessionProvider();
            _logger = logger;
        }

        /// <inheritdoc/>
        public virtual async Task<bool> IsOriginAllowedAsync(string origin)
        {
            origin = origin.ToLowerInvariant();

            bool isAllowed = await _documentSession
                .Query<TClientEntity>()
                .Where(client => client.AllowedCorsOrigins.Any(item => item == origin))
                .AnyAsync()
                .ConfigureAwait(false);

            _logger.LogDebug("Origin {origin} is allowed: {originAllowed}", origin, isAllowed);

            return isAllowed;
        }
    }
}