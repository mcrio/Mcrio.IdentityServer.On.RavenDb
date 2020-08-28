using System;
using AutoMapper;
using Mcrio.IdentityServer.On.RavenDb.Storage.Entities;
using Mcrio.IdentityServer.On.RavenDb.Storage.Extensions;
using Mcrio.IdentityServer.On.RavenDb.Storage.Mappers.Profiles;
using Mcrio.IdentityServer.On.RavenDb.Storage.RavenDb;
using Raven.Client.Documents;

namespace Mcrio.IdentityServer.On.RavenDb.Storage.Mappers
{
    public class IdentityServerStoreMapper : IIdentityServerStoreMapper
    {
        private readonly IDocumentStore _documentStore;
        private readonly IMapper _mapper;

        public IdentityServerStoreMapper(IIdentityServerDocumentSessionWrapper identityServerDocumentSessionWrapper)
        {
            _documentStore = identityServerDocumentSessionWrapper.Session.Advanced.DocumentStore;

            var mapperConfiguration = new MapperConfiguration(expression =>
            {
                expression.AddProfile(new ApiResourceMapperProfile(CreateEntityId<ApiResource>));
                expression.AddProfile(new ClientMapperProfile(CreateEntityId<Client>));
                expression.AddProfile(new IdentityResourceMapperProfile(CreateEntityId<IdentityResource>));
                expression.AddProfile(new PersistedGrantMapperProfile(CreateEntityId<PersistedGrant>));
                expression.AddProfile(new ScopeMapperProfile(CreateEntityId<ApiScope>));
            });
            _mapper = new Mapper(mapperConfiguration);
        }

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

            _mapper.Map(source, destination);
        }

        public void AssertConfigurationIsValid<TProfile>()
            where TProfile : Profile
        {
            _mapper.ConfigurationProvider.AssertConfigurationIsValid(typeof(TProfile).FullName);
        }

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

        public TModel ToModel<TEntity, TModel>(TEntity entity)
            where TEntity : IEntity
        {
            if (entity == null)
            {
                throw new ArgumentNullException(nameof(entity));
            }

            return _mapper.Map<TModel>(entity);
        }

        public TEntity ToEntity<TModel, TEntity>(TModel model)
            where TEntity : IEntity
        {
            if (model == null)
            {
                throw new ArgumentNullException(nameof(model));
            }

            return _mapper.Map<TEntity>(model);
        }
    }
}