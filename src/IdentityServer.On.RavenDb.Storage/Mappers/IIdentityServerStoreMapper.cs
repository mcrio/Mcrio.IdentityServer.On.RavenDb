using AutoMapper;
using Mcrio.IdentityServer.On.RavenDb.Storage.Entities;

namespace Mcrio.IdentityServer.On.RavenDb.Storage.Mappers
{
    public interface IIdentityServerStoreMapper
    {
        void Map<TSource, TDestination>(TSource source, TDestination destination);

        void AssertConfigurationIsValid();

        void AssertConfigurationIsValid<TProfile>()
            where TProfile : Profile;

        string CreateEntityId<TEntity>(string uniqueValue)
            where TEntity : IEntity;

        TModel ToModel<TEntity, TModel>(TEntity entity)
            where TEntity : IEntity;

        TEntity ToEntity<TModel, TEntity>(TModel model)
            where TEntity : IEntity;
    }
}