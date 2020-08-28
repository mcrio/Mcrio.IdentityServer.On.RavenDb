using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IdentityServer4.Models;
using IdentityServer4.Stores;
using Mcrio.IdentityServer.On.RavenDb.Storage.Mappers;
using Mcrio.IdentityServer.On.RavenDb.Storage.RavenDb;
using Microsoft.Extensions.Logging;
using Raven.Client.Documents;
using Raven.Client.Documents.Commands;
using Raven.Client.Documents.Linq;
using Raven.Client.Documents.Session;

namespace Mcrio.IdentityServer.On.RavenDb.Storage.Stores
{
    public class ResourceStore : ResourceStore<Entities.IdentityResource, Entities.ApiResource, Entities.ApiScope>
    {
        public ResourceStore(
            IIdentityServerDocumentSessionWrapper identityServerDocumentSessionWrapper,
            IIdentityServerStoreMapper mapper,
            ILogger<ResourceStore<Entities.IdentityResource, Entities.ApiResource, Entities.ApiScope>> logger)
            : base(identityServerDocumentSessionWrapper, mapper, logger)
        {
        }
    }

    public abstract class ResourceStore<TIdentityResourceEntity, TApiResourceEntity, TApiScopeEntity> : IResourceStore
        where TIdentityResourceEntity : Entities.IdentityResource
        where TApiResourceEntity : Entities.ApiResource
        where TApiScopeEntity : Entities.ApiScope

    {
        protected ResourceStore(
            IIdentityServerDocumentSessionWrapper identityServerDocumentSessionWrapper,
            IIdentityServerStoreMapper mapper,
            ILogger<ResourceStore<TIdentityResourceEntity, TApiResourceEntity, TApiScopeEntity>> logger)
        {
            DocumentSession = identityServerDocumentSessionWrapper.Session;
            Mapper = mapper;
            Logger = logger;
        }

        protected IAsyncDocumentSession DocumentSession { get; }

        protected IIdentityServerStoreMapper Mapper { get; }

        protected ILogger<ResourceStore<TIdentityResourceEntity, TApiResourceEntity, TApiScopeEntity>> Logger { get; }

        public virtual async Task<IEnumerable<IdentityResource>> FindIdentityResourcesByScopeNameAsync(
            IEnumerable<string> scopeNames)
        {
            if (scopeNames == null)
            {
                throw new ArgumentNullException(nameof(scopeNames));
            }

            List<string> ids = scopeNames
                .Select(scopeName => Mapper.CreateEntityId<TIdentityResourceEntity>(scopeName))
                .ToList();

            if (ids.Count == 0)
            {
                return new IdentityResource[0];
            }

            Dictionary<string, TIdentityResourceEntity> entitiesDict = await DocumentSession
                .LoadAsync<TIdentityResourceEntity>(ids)
                .ConfigureAwait(false);

            return entitiesDict
                .Values
                .Where(entity => entity != null)
                .Select(entity => Mapper.ToModel<TIdentityResourceEntity, IdentityResource>(entity));
        }

        public virtual async Task<IEnumerable<ApiScope>> FindApiScopesByNameAsync(IEnumerable<string> scopeNames)
        {
            if (scopeNames == null)
            {
                throw new ArgumentNullException(nameof(scopeNames));
            }

            List<string> ids = scopeNames
                .Select(scopeName => Mapper.CreateEntityId<TApiScopeEntity>(scopeName))
                .ToList();

            if (ids.Count == 0)
            {
                return new ApiScope[0];
            }

            Dictionary<string, TApiScopeEntity> entitiesDict = await DocumentSession
                .LoadAsync<TApiScopeEntity>(ids)
                .ConfigureAwait(false);

            return entitiesDict
                .Values
                .Where(entity => entity != null)
                .Select(entity => Mapper.ToModel<TApiScopeEntity, ApiScope>(entity));
        }

        public virtual async Task<IEnumerable<ApiResource>> FindApiResourcesByScopeNameAsync(
            IEnumerable<string> scopeNames)
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

            List<TApiResourceEntity> entities = await DocumentSession
                .Query<TApiResourceEntity>()
                .Where(resource => resource.Scopes.In(names))
                .ToListAsync()
                .ConfigureAwait(false);

            return entities.Select(entity => Mapper.ToModel<TApiResourceEntity, ApiResource>(entity));
        }

        public virtual async Task<IEnumerable<ApiResource>> FindApiResourcesByNameAsync(
            IEnumerable<string> apiResourceNames)
        {
            if (apiResourceNames == null)
            {
                throw new ArgumentNullException(nameof(apiResourceNames));
            }

            List<string> ids = apiResourceNames
                .Select(apiResourceName => Mapper.CreateEntityId<TApiResourceEntity>(apiResourceName))
                .ToList();

            if (ids.Count == 0)
            {
                return new ApiResource[0];
            }

            Dictionary<string, TApiResourceEntity> entitiesDict = await DocumentSession
                .LoadAsync<TApiResourceEntity>(ids)
                .ConfigureAwait(false);

            IEnumerable<ApiResource> resources = entitiesDict
                .Values
                .Where(entity => entity != null)
                .Select(entity => Mapper.ToModel<TApiResourceEntity, ApiResource>(entity));
            return resources;
        }

        public virtual async Task<Resources> GetAllResourcesAsync()
        {
            IRavenQueryable<TIdentityResourceEntity> identityResourcesQuery =
                DocumentSession.Query<TIdentityResourceEntity>();
            IRavenQueryable<TApiResourceEntity> apiResourceQuery = DocumentSession.Query<TApiResourceEntity>();
            IRavenQueryable<TApiScopeEntity> apiScopesQuery = DocumentSession.Query<TApiScopeEntity>();

            Raven.Client.Util.IAsyncEnumerator<StreamResult<TIdentityResourceEntity>> identityStreamResult =
                await DocumentSession
                    .Advanced
                    .StreamAsync(identityResourcesQuery)
                    .ConfigureAwait(false);
            var identityResources = new List<IdentityResource>();
            while (await identityStreamResult.MoveNextAsync().ConfigureAwait(false))
            {
                identityResources.Add(
                    Mapper.ToModel<TIdentityResourceEntity, IdentityResource>(identityStreamResult.Current.Document)
                );
            }

            Raven.Client.Util.IAsyncEnumerator<StreamResult<TApiResourceEntity>> apiResourceStreamResult =
                await DocumentSession
                    .Advanced
                    .StreamAsync(apiResourceQuery)
                    .ConfigureAwait(false);
            var apiResources = new List<ApiResource>();
            while (await apiResourceStreamResult.MoveNextAsync().ConfigureAwait(false))
            {
                apiResources.Add(
                    Mapper.ToModel<TApiResourceEntity, ApiResource>(apiResourceStreamResult.Current.Document)
                );
            }

            Raven.Client.Util.IAsyncEnumerator<StreamResult<TApiScopeEntity>> apiScopeStreamResult =
                await DocumentSession
                    .Advanced
                    .StreamAsync(apiScopesQuery)
                    .ConfigureAwait(false);
            var apiScopes = new List<ApiScope>();
            while (await apiScopeStreamResult.MoveNextAsync().ConfigureAwait(false))
            {
                apiScopes.Add(Mapper.ToModel<TApiScopeEntity, ApiScope>(apiScopeStreamResult.Current.Document));
            }

            return new Resources(identityResources, apiResources, apiScopes);
        }
    }
}