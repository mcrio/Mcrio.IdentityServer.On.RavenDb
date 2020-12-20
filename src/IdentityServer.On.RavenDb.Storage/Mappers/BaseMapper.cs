using System.Collections.Generic;
using AutoMapper;

namespace Mcrio.IdentityServer.On.RavenDb.Storage.Mappers
{
    /// <summary>
    /// Base mapper class.
    /// </summary>
    public abstract class BaseMapper
    {
        private IMapper? _mapper;

        /// <summary>
        /// Gets the Automapper instance.
        /// </summary>
        protected IMapper Mapper
        {
            get
            {
                return _mapper ??= new Mapper(
                    new MapperConfiguration(expression =>
                    {
                        expression.AddProfiles(GetMapperProfiles());
                    })
                );
            }
        }

        /// <summary>
        /// Assert configuration.
        /// </summary>
        /// <typeparam name="TProfile">Profile type.</typeparam>
        public void AssertConfigurationIsValid<TProfile>()
            where TProfile : Profile
        {
            Mapper.ConfigurationProvider.AssertConfigurationIsValid(typeof(TProfile).FullName);
        }

        /// <summary>
        /// Assert configuration.
        /// </summary>
        public void AssertConfigurationIsValid()
        {
            Mapper.ConfigurationProvider.AssertConfigurationIsValid();
        }

        /// <summary>
        /// Get mapper profiles.
        /// </summary>
        /// <returns>Returns a collection of mapper profiles.</returns>
        protected abstract IEnumerable<Profile> GetMapperProfiles();
    }
}