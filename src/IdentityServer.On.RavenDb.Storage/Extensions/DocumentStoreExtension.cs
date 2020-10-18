using System;
using Raven.Client.Documents;

namespace Mcrio.IdentityServer.On.RavenDb.Storage.Extensions
{
    internal static class DocumentStoreExtension
    {
        internal static string GetCollectionName(this IDocumentStore documentStore, Type entityType)
        {
            return documentStore.Conventions.GetCollectionName(entityType);
        }

        internal static string GetCollectionPrefix(this IDocumentStore documentStore, Type entityType)
        {
            string collectionName = GetCollectionName(documentStore, entityType);
            string prefix = documentStore
                .Conventions
                .TransformTypeCollectionNameToDocumentIdPrefix(collectionName);
            return prefix;
        }

        internal static string GetCollectionPrefixWithSeparator(this IDocumentStore documentStore, Type entityType)
        {
            string prefix = GetCollectionPrefix(documentStore, entityType);
            char separator = documentStore
                .Conventions
                .IdentityPartsSeparator;
            return $"{prefix}{separator}";
        }
    }
}