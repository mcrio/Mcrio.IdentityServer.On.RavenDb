using System;
using Raven.Client.Documents;

namespace Mcrio.IdentityServer.On.RavenDb.Storage.Extensions
{
    internal static class DocumentStoreExtension
    {
        internal static string GetCollectionPrefix(this IDocumentStore documentStore, Type entityType)
        {
            var collectionName = documentStore.Conventions.GetCollectionName(entityType);
            var prefix = documentStore
                .Conventions
                .TransformTypeCollectionNameToDocumentIdPrefix(collectionName);
            return prefix;
        }

        internal static string GetCollectionPrefixWithSeparator(this IDocumentStore documentStore, Type entityType)
        {
            var prefix = GetCollectionPrefix(documentStore, entityType);
            var separator = documentStore
                .Conventions
                .IdentityPartsSeparator;
            return $"{prefix}{separator}";
        }
    }
}