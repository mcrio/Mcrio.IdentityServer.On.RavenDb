using System;
using Mcrio.IdentityServer.On.RavenDb.Storage.Entities;

namespace Mcrio.IdentityServer.On.RavenDb.Storage.RavenDb
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
                collectionName = "ApiClients";
                return true;
            }

            if (typeof(ApiResource).IsAssignableFrom(type))
            {
                collectionName = "ApiResources";
                return true;
            }

            if (typeof(ApiScope).IsAssignableFrom(type))
            {
                collectionName = "ApiScopes";
                return true;
            }

            if (typeof(DeviceFlowCode).IsAssignableFrom(type))
            {
                collectionName = "ApiDeviceFlows";
                return true;
            }

            if (typeof(IdentityResource).IsAssignableFrom(type))
            {
                collectionName = "ApiIdentResources";
                return true;
            }

            if (typeof(PersistedGrant).IsAssignableFrom(type))
            {
                collectionName = "ApiGrants";
                return true;
            }

            collectionName = null;
            return false;
        }
    }
}