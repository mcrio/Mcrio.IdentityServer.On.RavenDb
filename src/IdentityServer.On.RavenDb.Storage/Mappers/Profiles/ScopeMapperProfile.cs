using System;
using AutoMapper;
using Models = IdentityServer4.Models;

namespace Mcrio.IdentityServer.On.RavenDb.Storage.Mappers.Profiles
{
    /// <summary>
    /// Defines entity/model mapping for scopes.
    /// </summary>
    /// <seealso cref="AutoMapper.Profile" />
    public class ScopeMapperProfile : Profile
    {
        /// <summary>
        /// <see cref="ScopeMapperProfile"/>
        /// </summary>
        public ScopeMapperProfile(Func<string, string> apiScopeNameToEntityIdMapper)
        {
            CreateMap<Entities.ApiScope, Entities.ApiScope>();

            CreateMap<Entities.ApiScope, Models.ApiScope>(MemberList.Destination)
                .ConstructUsing(src => new IdentityServer4.Models.ApiScope())
                .ReverseMap()
                .ForMember(
                    dest => dest.Id,
                    opt =>
                        opt.MapFrom(src => apiScopeNameToEntityIdMapper(src.Name))
                );
        }
    }
}