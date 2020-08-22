using System;
using AutoMapper;
using Models = IdentityServer4.Models;

namespace Mcrio.IdentityServer.On.RavenDb.Storage.Mappers.Profiles
{
    /// <summary>
    /// Defines entity/model mapping for API resources.
    /// </summary>
    /// <seealso cref="AutoMapper.Profile" />
    public class ApiResourceMapperProfile : Profile
    {
        /// <summary>
        /// <see cref="ApiResourceMapperProfile"/>
        /// </summary>
        public ApiResourceMapperProfile(Func<string, string> apiResourceNameToEntityId)
        {
            CreateMap<Entities.ApiResource, Entities.ApiResource>();

            CreateMap<Entities.ApiResource, Models.ApiResource>(MemberList.Destination)
                .ConstructUsing(src => new IdentityServer4.Models.ApiResource())
                .ForMember(
                    x => x.ApiSecrets,
                    opts => opts.MapFrom(x => x.Secrets)
                )
                .ReverseMap()
                .ForMember(
                    dest => dest.Id,
                    opt => opt.MapFrom(src => apiResourceNameToEntityId(src.Name)));

            CreateMap<Entities.ApiResourceSecret, Models.Secret>(MemberList.Destination)
                .ForMember(dest => dest.Type, opt => opt.Condition(srs => srs != null))
                .ReverseMap();
        }
    }
}