using System;
using Raven.Client;
using Raven.Client.Documents.Operations.CompareExchange;
using Raven.Client.Documents.Session;

namespace Mcrio.IdentityServer.On.RavenDb.Storage.Stores.Extensions
{
    /// <summary>
    /// RavenDb metadata extension methods.
    /// </summary>
    internal static class RavenDbDocumentMetadataExtensions
    {
        /// <summary>
        /// Sets or removes the RavenDb document `@expires` metadata.
        /// </summary>
        /// <param name="documentSession">Session.</param>
        /// <param name="cleanupOptions">Clean up options.</param>
        /// <param name="document">Document.</param>
        /// <param name="expiration">Expiration time.</param>
        internal static void ManageDocumentExpiresMetadata(
            this IAsyncDocumentSession documentSession,
            OperationalStoreOptions cleanupOptions,
            object document,
            DateTime? expiration)
        {
            if (cleanupOptions.SetRavenDbDocumentExpiresMetadata == false)
            {
                return;
            }

            IMetadataDictionary metadata = documentSession.Advanced.GetMetadataFor(document);
            if (expiration is null)
            {
                metadata.Remove(Constants.Documents.Metadata.Expires);
            }
            else
            {
                metadata[Constants.Documents.Metadata.Expires] = expiration.Value.ToUniversalTime();
            }
        }

        /// <summary>
        /// Sets or removes the RavenDb compare exchange `@expires` metadata.
        /// </summary>
        /// <param name="compareExchangeValue">Compare exchange value.</param>
        /// <param name="cleanupOptions">Operational store cleanup options.</param>
        /// <param name="expirationTime">Document expiration time.</param>
        /// <typeparam name="TValue">Compare exchange value type.</typeparam>
        internal static void ManageCompareExchangeExpiresMetadata<TValue>(
            this CompareExchangeValue<TValue> compareExchangeValue,
            OperationalStoreOptions cleanupOptions,
            DateTime? expirationTime)
        {
            if (cleanupOptions.SetRavenDbDocumentExpiresMetadata == false)
            {
                return;
            }

            IMetadataDictionary? metadata = compareExchangeValue.Metadata;

            if (expirationTime is null)
            {
                metadata.Remove(Constants.Documents.Metadata.Expires);
            }
            else
            {
                metadata[Constants.Documents.Metadata.Expires] = expirationTime.Value.ToUniversalTime();
            }
        }
    }
}