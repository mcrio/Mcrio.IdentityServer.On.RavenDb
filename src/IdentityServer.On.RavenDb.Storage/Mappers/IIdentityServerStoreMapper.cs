using AutoMapper;
using Models = IdentityServer4.Models;

namespace Mcrio.IdentityServer.On.RavenDb.Storage.Mappers
{
    public interface IIdentityServerStoreMapper
    {
        void Map<TSource, TDestination>(TSource source, TDestination destination);

        void AssertConfigurationIsValid<TProfile>() where TProfile : Profile;

        string CreateEntityId<TEntity>(string uniqueValue) where TEntity : class;

        Entities.Client ToEntity(Models.Client model);

        Models.Client ToModel(Entities.Client entity);

        Entities.ApiResource ToEntity(Models.ApiResource model);

        Models.ApiResource ToModel(Entities.ApiResource entity);

        Entities.IdentityResource ToEntity(Models.IdentityResource model);

        Models.IdentityResource ToModel(Entities.IdentityResource entity);

        Entities.ApiScope ToEntity(Models.ApiScope model);

        Models.ApiScope ToModel(Entities.ApiScope entity);

        Entities.PersistedGrant ToEntity(Models.PersistedGrant model);

        Models.PersistedGrant ToModel(Entities.PersistedGrant entity);
    }
}