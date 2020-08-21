using System;
using AutoMapper;
using Mcrio.IdentityServer.On.RavenDb.Storage.Extensions;
using Mcrio.IdentityServer.On.RavenDb.Storage.Mappers.Profiles;
using Raven.Client.Documents;
using Models = IdentityServer4.Models;

namespace Mcrio.IdentityServer.On.RavenDb.Storage.Mappers
{
    internal class IdentityServerStoreMapper : IIdentityServerStoreMapper
    {
        private readonly IDocumentStore _documentStore;
        private readonly IMapper _mapper;

        public IdentityServerStoreMapper(IdentityServerDocumentSessionProvider identityServerDocumentSessionProvider)
        {
            _documentStore = identityServerDocumentSessionProvider().Advanced.DocumentStore;

            var mapperConfiguration = new MapperConfiguration(expression =>
            {
                expression.AddProfile(new ApiResourceMapperProfile(CreateEntityId<Entities.ApiResource>));
                expression.AddProfile(new ClientMapperProfile(CreateEntityId<Entities.Client>));
                expression.AddProfile(new IdentityResourceMapperProfile(CreateEntityId<Entities.IdentityResource>));
                expression.AddProfile(new PersistedGrantMapperProfile(CreateEntityId<Entities.PersistedGrant>));
                expression.AddProfile(new ScopeMapperProfile(CreateEntityId<Entities.ApiScope>));
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

        public void AssertConfigurationIsValid<TProfile>() where TProfile : Profile
        {
            _mapper.ConfigurationProvider.AssertConfigurationIsValid(typeof(TProfile).FullName);
        }

        public string CreateEntityId<TEntity>(string uniqueValue) where TEntity : class
        {
            if (string.IsNullOrWhiteSpace(uniqueValue))
            {
                throw new ArgumentNullException(nameof(uniqueValue));
            }

            var prefixWithSeparator = _documentStore.GetCollectionPrefixWithSeparator(typeof(TEntity));
            return $"{prefixWithSeparator}{uniqueValue}";
        }

        public Entities.Client ToEntity(Models.Client model)
        {
            if (model == null)
            {
                throw new ArgumentNullException(nameof(model));
            }

            return _mapper.Map<Entities.Client>(model);
        }

        public Models.Client ToModel(Entities.Client entity)
        {
            if (entity == null)
            {
                throw new ArgumentNullException(nameof(entity));
            }

            return _mapper.Map<Models.Client>(entity);
        }

        public Entities.ApiResource ToEntity(Models.ApiResource model)
        {
            if (model == null)
            {
                throw new ArgumentNullException(nameof(model));
            }

            return _mapper.Map<Entities.ApiResource>(model);
        }

        public Models.ApiResource ToModel(Entities.ApiResource entity)
        {
            if (entity == null)
            {
                throw new ArgumentNullException(nameof(entity));
            }

            return _mapper.Map<Models.ApiResource>(entity);
        }

        public Entities.IdentityResource ToEntity(Models.IdentityResource model)
        {
            if (model == null)
            {
                throw new ArgumentNullException(nameof(model));
            }

            return _mapper.Map<Entities.IdentityResource>(model);
        }

        public Models.IdentityResource ToModel(Entities.IdentityResource entity)
        {
            if (entity == null)
            {
                throw new ArgumentNullException(nameof(entity));
            }

            return _mapper.Map<Models.IdentityResource>(entity);
        }

        public Entities.ApiScope ToEntity(Models.ApiScope model)
        {
            if (model == null)
            {
                throw new ArgumentNullException(nameof(model));
            }

            return _mapper.Map<Entities.ApiScope>(model);
        }

        public Models.ApiScope ToModel(Entities.ApiScope entity)
        {
            if (entity == null)
            {
                throw new ArgumentNullException(nameof(entity));
            }

            return _mapper.Map<Models.ApiScope>(entity);
        }

        public Entities.PersistedGrant ToEntity(Models.PersistedGrant model)
        {
            if (model == null)
            {
                throw new ArgumentNullException(nameof(model));
            }

            return _mapper.Map<Entities.PersistedGrant>(model);
        }

        public Models.PersistedGrant ToModel(Entities.PersistedGrant entity)
        {
            if (entity == null)
            {
                throw new ArgumentNullException(nameof(entity));
            }

            return _mapper.Map<Models.PersistedGrant>(entity);
        }
    }
}