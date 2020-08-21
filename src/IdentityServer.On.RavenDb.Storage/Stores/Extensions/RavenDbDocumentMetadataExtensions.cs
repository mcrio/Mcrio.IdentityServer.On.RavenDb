using System;
using Mcrio.IdentityServer.On.RavenDb.Storage.TokenCleanup;
using Raven.Client;
using Raven.Client.Documents.Session;

namespace Mcrio.IdentityServer.On.RavenDb.Storage.Stores.Extensions
{
    internal static class RavenDbDocumentMetadataExtensions
    {
        internal static void ManageDocumentExpiresMetadata(
            this IAsyncDocumentSession documentSession,
            OperationalStoreOptions cleanupOptions,
            object entity,
            DateTime? expiration)
        {
            if (cleanupOptions.SetRavenDbDocumentExpiresMetadata == false)
            {
                return;
            }

            IMetadataDictionary metadata = documentSession.Advanced.GetMetadataFor(entity);
            if (expiration is null)
            {
                metadata.Remove(Constants.Documents.Metadata.Expires);
            }
            else
            {
                metadata[Constants.Documents.Metadata.Expires] = expiration.Value.ToUniversalTime();
            }
        }
    }
}