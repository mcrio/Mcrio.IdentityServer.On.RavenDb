using System;
using Mcrio.IdentityServer.On.RavenDb.Storage.Entities;

namespace Mcrio.IdentityServer.On.RavenDb.Storage
{
    /// <summary>
    /// Method to produce predefined collection names for implemented entity types.
    /// </summary>
    public static class IdentityServerRavenDbConventions
    {
        /// <summary>
        /// Get collection name for Identity Server on RavenDb known types.
        /// </summary>
        /// <param name="type">Object type to get the collection for.</param>
        /// <param name="collectionName">Optional collection name if found.</param>
        /// <returns>Default collection name if known type otherwise Null.</returns>
        public static bool TryGetCollectionName(Type type, out string? collectionName)
        {
            if (typeof(Client).IsAssignableFrom(type))
            {
                collectionName = "Apiclients";
                return true;
            }

            if (typeof(ApiResource).IsAssignableFrom(type))
            {
                collectionName = "Apiresources";
                return true;
            }

            if (typeof(ApiScope).IsAssignableFrom(type))
            {
                collectionName = "Apiscopes";
                return true;
            }

            if (typeof(DeviceFlowCode).IsAssignableFrom(type))
            {
                collectionName = "Apideviceflows";
                return true;
            }

            if (typeof(IdentityResource).IsAssignableFrom(type))
            {
                collectionName = "Apiidentresources";
                return true;
            }

            if (typeof(PersistedGrant).IsAssignableFrom(type))
            {
                collectionName = "Apigrants";
                return true;
            }

            collectionName = null;
            return false;
        }
    }
}