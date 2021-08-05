using System;
using System.Collections.Generic;
using AutoMapper;
using Mcrio.IdentityServer.On.RavenDb.Storage.Entities;
using Mcrio.IdentityServer.On.RavenDb.Storage.Extensions;
using Mcrio.IdentityServer.On.RavenDb.Storage.Mappers.Profiles;
using Raven.Client.Documents;

namespace Mcrio.IdentityServer.On.RavenDb.Storage.Mappers
{
    /// <summary>
    /// Identity server entities to store model mapper.
    /// </summary>
    public class IdentityServerStoreMapper : BaseMapper, IIdentityServerStoreMapper
    {
        private readonly IDocumentStore _documentStore;

        /// <summary>
        /// Initializes a new instance of the <see cref="IdentityServerStoreMapper"/> class.
        /// </summary>
        /// <param name="documentStore">Document store.</param>
        public IdentityServerStoreMapper(IDocumentStore documentStore)
        {
            _documentStore = documentStore;
        }

        /// <inheritdoc />
        public void Map<TSource, TDestination>(TSource source, TDestination destination)
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
        public string CreateEntityId<TEntity>(string uniqueValue)
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
        public TModel ToModel<TEntity, TModel>(TEntity entity)
            where TEntity : IEntity
        {
            if (entity == null)
            {
                throw new ArgumentNullException(nameof(entity));
            }

            return Mapper.Map<TModel>(entity);
        }

        /// <inheritdoc />
        public TEntity ToEntity<TModel, TEntity>(TModel model)
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
                new ApiResourceMapperProfile(CreateEntityId<ApiResource>),
                new ClientMapperProfile(CreateEntityId<Client>),
                new IdentityResourceMapperProfile(CreateEntityId<IdentityResource>),
                new PersistedGrantMapperProfile(CreateEntityId<PersistedGrant>),
                new ScopeMapperProfile(CreateEntityId<ApiScope>),
            };
        }
    }
}