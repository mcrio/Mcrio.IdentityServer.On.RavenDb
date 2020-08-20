using System;
using System.Security.Claims;
using AutoMapper;
using Models = IdentityServer4.Models;

namespace Mcrio.IdentityServer.On.RavenDb.Storage.Mappers.Profiles
{
    /// <summary>
    /// Defines entity/model mapping for clients.
    /// </summary>
    /// <seealso cref="AutoMapper.Profile" />
    public class ClientMapperProfile : Profile
    {
        /// <summary>
        /// <see>
        ///     <cref>{ClientMapperProfile}</cref>
        /// </see>
        /// </summary>
        public ClientMapperProfile(Func<string, string> clientIdToEntityIdMapper)
        {
            CreateMap<Entities.Client, Entities.Client>();

            CreateMap<Entities.Client, Models.Client>()
                .ForMember(dest => dest.ProtocolType, opt => opt.Condition(srs => srs != null))
                .ReverseMap()
                .ForMember(
                    dest => dest.Id,
                    opt => opt.MapFrom(src => clientIdToEntityIdMapper(src.ClientId)));

            CreateMap<Entities.ClientClaim, Models.ClientClaim>(MemberList.None)
                .ConstructUsing(src => new Models.ClientClaim(src.Type, src.Value, ClaimValueTypes.String))
                .ReverseMap();

            CreateMap<Entities.ClientSecret, Models.Secret>(MemberList.Destination)
                .ForMember(dest => dest.Type, opt => opt.Condition(srs => srs != null))
                .ReverseMap();
        }
    }
}