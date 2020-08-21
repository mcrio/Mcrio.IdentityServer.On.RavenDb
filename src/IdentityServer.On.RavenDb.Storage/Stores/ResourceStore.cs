using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IdentityServer4.Models;
using IdentityServer4.Stores;
using Mcrio.IdentityServer.On.RavenDb.Storage.Mappers;
using Mcrio.IdentityServer.On.RavenDb.Storage.Stores.Advanced;
using Microsoft.Extensions.Logging;
using Raven.Client.Documents;
using Raven.Client.Documents.Linq;
using Raven.Client.Documents.Session;
using Raven.Client.Exceptions;

namespace Mcrio.IdentityServer.On.RavenDb.Storage.Stores
{
    public class ResourceStore : IResourceStore
    {
        private readonly IAsyncDocumentSession _documentSession;
        private readonly IIdentityServerStoreMapper _mapper;
        private readonly ILogger<ResourceStore> _logger;

        public ResourceStore(
            IdentityServerDocumentSessionProvider identityServerDocumentSessionProvider,
            IIdentityServerStoreMapper mapper,
            ILogger<ResourceStore> logger)
        {
            _documentSession = identityServerDocumentSessionProvider();
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<IEnumerable<IdentityResource>> FindIdentityResourcesByScopeNameAsync(
            IEnumerable<string> scopeNames)
        {
            if (scopeNames == null)
            {
                throw new ArgumentNullException(nameof(scopeNames));
            }

            List<string> ids = scopeNames
                .Select(scopeName => _mapper.CreateEntityId<Entities.IdentityResource>(scopeName))
                .ToList();

            if (ids.Count == 0)
            {
                return new IdentityResource[0];
            }

            Dictionary<string, Entities.IdentityResource> entitiesDict = await _documentSession
                .LoadAsync<Entities.IdentityResource>(ids)
                .ConfigureAwait(false);

            return entitiesDict
                .Values
                .Where(entity => entity != null)
                .Select(entity => _mapper.ToModel(entity));
        }

        public async Task<IEnumerable<ApiScope>> FindApiScopesByNameAsync(IEnumerable<string> scopeNames)
        {
            if (scopeNames == null)
            {
                throw new ArgumentNullException(nameof(scopeNames));
            }

            List<string> ids = scopeNames
                .Select(scopeName => _mapper.CreateEntityId<Entities.ApiScope>(scopeName))
                .ToList();

            if (ids.Count == 0)
            {
                return new ApiScope[0];
            }

            Dictionary<string, Entities.ApiScope> entitiesDict = await _documentSession
                .LoadAsync<Entities.ApiScope>(ids)
                .ConfigureAwait(false);

            return entitiesDict
                .Values
                .Where(entity => entity != null)
                .Select(entity => _mapper.ToModel(entity));
        }

        public async Task<IEnumerable<ApiResource>> FindApiResourcesByScopeNameAsync(IEnumerable<string> scopeNames)
        {
            if (scopeNames == null)
            {
                throw new ArgumentNullException(nameof(scopeNames));
            }

            List<string> names = scopeNames.ToList();

            if (names.Count == 0)
            {
                return new ApiResource[0];
            }

            List<Entities.ApiResource> entities = await _documentSession
                .Query<Entities.ApiResource>()
                .Where(resource => resource.Scopes.In(names))
                .ToListAsync()
                .ConfigureAwait(false);

            return entities.Select(entity => _mapper.ToModel(entity));
        }

        public async Task<IEnumerable<ApiResource>> FindApiResourcesByNameAsync(IEnumerable<string> apiResourceNames)
        {
            if (apiResourceNames == null)
            {
                throw new ArgumentNullException(nameof(apiResourceNames));
            }

            List<string> ids = apiResourceNames
                .Select(apiResourceName => _mapper.CreateEntityId<Entities.ApiResource>(apiResourceName))
                .ToList();

            if (ids.Count == 0)
            {
                return new ApiResource[0];
            }

            Dictionary<string, Entities.ApiResource> entitiesDict = await _documentSession
                .LoadAsync<Entities.ApiResource>(ids)
                .ConfigureAwait(false);

            var resources = entitiesDict
                .Values
                .Where(entity => entity != null)
                .Select(entity => _mapper.ToModel(entity));
            return resources;
        }

        public async Task<Resources> GetAllResourcesAsync()
        {
            var identityResourcesQuery = _documentSession.Query<Entities.IdentityResource>();
            var apiResourceQuery = _documentSession.Query<Entities.ApiResource>();
            var apiScopesQuery = _documentSession.Query<Entities.ApiScope>();

            var identityStreamResult = await _documentSession
                .Advanced
                .StreamAsync(identityResourcesQuery)
                .ConfigureAwait(false);
            var identityResources = new List<IdentityResource>();
            while (await identityStreamResult.MoveNextAsync().ConfigureAwait(false))
            {
                identityResources.Add(_mapper.ToModel(identityStreamResult.Current.Document));
            }

            var apiResourceStreamResult = await _documentSession
                .Advanced
                .StreamAsync(apiResourceQuery)
                .ConfigureAwait(false);
            var apiResources = new List<ApiResource>();
            while (await apiResourceStreamResult.MoveNextAsync().ConfigureAwait(false))
            {
                apiResources.Add(_mapper.ToModel(apiResourceStreamResult.Current.Document));
            }

            var apiScopeStreamResult = await _documentSession
                .Advanced
                .StreamAsync(apiScopesQuery)
                .ConfigureAwait(false);
            var apiScopes = new List<ApiScope>();
            while (await apiScopeStreamResult.MoveNextAsync().ConfigureAwait(false))
            {
                apiScopes.Add(_mapper.ToModel(apiScopeStreamResult.Current.Document));
            }

            return new Resources(identityResources, apiResources, apiScopes);
        }
    }
}