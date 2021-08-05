using System;
using AutoMapper;
using Mcrio.IdentityServer.On.RavenDb.Storage.Entities;

namespace Mcrio.IdentityServer.On.RavenDb.Storage.Mappers.Profiles
{
    /// <summary>
    /// Defines entity/model mapping for identity resources.
    /// </summary>
    /// <seealso cref="AutoMapper.Profile" />
    public class IdentityResourceMapperProfile : Profile
    {
        /// <summary>
        /// <see cref="IdentityResourceMapperProfile"/>
        /// </summary>
        public IdentityResourceMapperProfile(Func<string, string> identityResourceNameToEntityId)
        {
            CreateMap<IdentityResource, IdentityResource>();

            CreateMap<IdentityResource, IdentityServer4.Models.IdentityResource>(MemberList.Destination)
                .ConstructUsing(src => new IdentityServer4.Models.IdentityResource())
                .ReverseMap()
                .ForMember(
                    dest => dest.Id,
                    opt =>
                        opt.MapFrom(src => identityResourceNameToEntityId(src.Name))
                );
        }
    }
}