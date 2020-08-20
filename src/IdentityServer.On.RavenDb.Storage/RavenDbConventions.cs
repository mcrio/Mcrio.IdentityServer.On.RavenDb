using System;
using Mcrio.IdentityServer.On.RavenDb.Storage.Entities;

namespace Mcrio.IdentityServer.On.RavenDb.Storage
{
    /// <summary>
    /// Method to produce predefined collection names for implemented entity types.
    /// </summary>
    public static class RavenDbConventions
    {
        /// <summary>
        /// Get collection name for Identity Server on RavenDb known types.
        /// </summary>
        /// <param name="type">Object type to get the collection for.</param>
        /// <returns>Default collection name if known type otherwise Null.</returns>
        public static string? GetIdentityServerCollectionName(Type type)
        {
            if (typeof(Client).IsAssignableFrom(type))
            {
                return "Apiclients";
            }

            if (typeof(ApiResource).IsAssignableFrom(type))
            {
                return "Apiresources";
            }

            if (typeof(ApiScope).IsAssignableFrom(type))
            {
                return "Apiscopes";
            }

            if (typeof(DeviceFlowCode).IsAssignableFrom(type))
            {
                return "Apideviceflowcodes";
            }

            if (typeof(IdentityResource).IsAssignableFrom(type))
            {
                return "Apiidentresources";
            }

            if (typeof(PersistedGrant).IsAssignableFrom(type))
            {
                return "Apigrants";
            }

            return null;
        }
    }
}