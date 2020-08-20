using System;
using AutoMapper;
using Models = IdentityServer4.Models;

namespace Mcrio.IdentityServer.On.RavenDb.Storage.Mappers.Profiles
{
    /// <summary>
    /// Defines entity/model mapping for persisted grants.
    /// </summary>
    /// <seealso cref="AutoMapper.Profile" />
    public class PersistedGrantMapperProfile : Profile
    {
        /// <summary>
        /// <see cref="PersistedGrantMapperProfile">
        /// </see>
        /// </summary>
        public PersistedGrantMapperProfile(Func<string, string> persistedGrantKeyToEntityIdMapper)
        {
            CreateMap<Entities.PersistedGrant, Entities.PersistedGrant>();

            CreateMap<Entities.PersistedGrant, Models.PersistedGrant>(MemberList.Destination)
                .ReverseMap()
                .ForMember(
                    dest => dest.Id,
                    opt =>
                        opt.MapFrom(src => persistedGrantKeyToEntityIdMapper(src.Key))
                );
        }
    }
}