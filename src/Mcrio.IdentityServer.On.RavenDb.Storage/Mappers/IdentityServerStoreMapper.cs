using System;
using System.Collections.Generic;
using AutoMapper;
using Mcrio.IdentityServer.On.RavenDb.Storage.Entities;
using Mcrio.IdentityServer.On.RavenDb.Storage.Extensions;
using Mcrio.IdentityServer.On.RavenDb.Storage.Mappers.Profiles;
using Raven.Client.Documents;

namespace Mcrio.IdentityServer.On.RavenDb.Storage.Mappers
{
    /// <inheritdoc />
    public class IdentityServerStoreMapper
        : IdentityServerStoreMapper<
            ApiResource,
            Client,
            IdentityResource,
            PersistedGrant,
            ApiScope>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="IdentityServerStoreMapper"/> class.
        /// </summary>
        /// <param name="documentStore"></param>
        public IdentityServerStoreMapper(IDocumentStore documentStore)
            : base(documentStore)
        {
        }
    }

    /// <summary>
    /// Identity server entities to store model mapper.
    /// </summary>
    /// <typeparam name="TApiResource">Api resource type.</typeparam>
    /// <typeparam name="TClient">Api client type.</typeparam>
    /// <typeparam name="TIdentityResource">Api identity resource type.</typeparam>
    /// <typeparam name="TPersistedGrant">Api persisted grant type.</typeparam>
    /// <typeparam name="TApiScope">Api scope type.</typeparam>
    public abstract class IdentityServerStoreMapper<
        TApiResource,
        TClient,
        TIdentityResource,
        TPersistedGrant,
        TApiScope> : BaseMapper, IIdentityServerStoreMapper
        where TApiResource : ApiResource
        where TClient : Client
        where TIdentityResource : IdentityResource
        where TPersistedGrant : PersistedGrant
        where TApiScope : ApiScope
    {
        private readonly IDocumentStore _documentStore;

        /// <summary>
        /// Initializes a new instance of the <see cref="IdentityServerStoreMapper"/> class.
        /// </summary>
        /// <param name="documentStore">Document store.</param>
        protected IdentityServerStoreMapper(IDocumentStore documentStore)
        {
            _documentStore = documentStore;
        }

        /// <inheritdoc />
        public virtual void Map<TSource, TDestination>(TSource source, TDestination destination)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (destination == null)
            {
                throw new ArgumentNullException(nameof(destination));
            }

            Mapper.Map(source, destination);
        }

        /// <inheritdoc />
        public virtual string CreateEntityId<TEntity>(string uniqueValue)
            where TEntity : IEntity
        {
            if (string.IsNullOrWhiteSpace(uniqueValue))
            {
                throw new ArgumentNullException(nameof(uniqueValue));
            }

            string prefixWithSeparator = _documentStore.GetCollectionPrefixWithSeparator(typeof(TEntity));
            return $"{prefixWithSeparator}{uniqueValue}";
        }

        /// <inheritdoc />
        public virtual TModel ToModel<TEntity, TModel>(TEntity entity)
            where TEntity : IEntity
        {
            if (entity == null)
            {
                throw new ArgumentNullException(nameof(entity));
            }

            return Mapper.Map<TModel>(entity);
        }

        /// <inheritdoc />
        public virtual TEntity ToEntity<TModel, TEntity>(TModel model)
            where TEntity : IEntity
        {
            if (model == null)
            {
                throw new ArgumentNullException(nameof(model));
            }

            return Mapper.Map<TEntity>(model);
        }

        /// <inheritdoc />
        protected override IEnumerable<Profile> GetMapperProfiles()
        {
            return new Profile[]
            {
                new ApiResourceMapperProfile(CreateEntityId<TApiResource>),
                new ClientMapperProfile(CreateEntityId<TClient>),
                new IdentityResourceMapperProfile(CreateEntityId<TIdentityResource>),
                new PersistedGrantMapperProfile(CreateEntityId<TPersistedGrant>),
                new ScopeMapperProfile(CreateEntityId<TApiScope>),
            };
        }
    }
}