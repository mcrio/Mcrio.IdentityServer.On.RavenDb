using System;
using Raven.Client.Documents;

namespace Mcrio.IdentityServer.On.RavenDb.Storage.Extensions
{
    internal static class DocumentStoreExtension
    {
        internal static string GetCollectionPrefix(this IDocumentStore documentStore, Type entityType)
        {
            string collectionName = documentStore.Conventions.GetCollectionName(entityType);
            string prefix = documentStore
                .Conventions
                .TransformTypeCollectionNameToDocumentIdPrefix(collectionName);
            return prefix;
        }

        internal static string GetCollectionPrefixWithSeparator(this IDocumentStore documentStore, Type entityType)
        {
            string prefix = GetCollectionPrefix(documentStore, entityType);
            string separator = documentStore
                .Conventions
                .IdentityPartsSeparator;
            return $"{prefix}{separator}";
        }
    }
}