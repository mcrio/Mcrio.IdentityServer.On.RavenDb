using System;
using System.Threading.Tasks;
using Raven.Client.Documents;
using Raven.Client.Documents.Operations.CompareExchange;
using Raven.Client.Documents.Session;

namespace Mcrio.IdentityServer.On.RavenDb.Storage.Stores.Utility
{
    /// <summary>
    /// Provides extension methods to handle RavenDb compare exchange functionality.
    /// </summary>
    public class CompareExchangeUtility
    {
        private readonly IAsyncDocumentSession _documentSession;

        /// <summary>
        /// Initializes a new instance of the <see cref="CompareExchangeUtility"/> class.
        /// </summary>
        /// <param name="documentSession"></param>
        public CompareExchangeUtility(IAsyncDocumentSession documentSession)
        {
            _documentSession = documentSession;
        }

        /// <summary>
        /// Creates the compare exchange key for th given reservation type, entity and unique value.
        /// </summary>
        /// <param name="reservationType">Type of reservation.</param>
        /// <param name="expectedUniqueValue">The unique value.</param>
        /// <returns>The complete compare exchange key.</returns>
        public virtual string CreateCompareExchangeKey(
            UniqueReservationType reservationType,
            string expectedUniqueValue)
        {
            if (string.IsNullOrWhiteSpace(expectedUniqueValue))
            {
                throw new ArgumentException(
                    $"Unexpected empty value for {nameof(expectedUniqueValue)} in {nameof(CreateCompareExchangeKey)}");
            }

            string prefix = reservationType switch
            {
                UniqueReservationType.DeviceCode => "idsrv/devcode",
                _ => throw new Exception($"Unhandled reservation type {reservationType}")
            };
            return $"{prefix.TrimEnd('/')}/{expectedUniqueValue}";
        }

        /// <summary>
        /// Loads the compare exchange value by given key.
        /// </summary>
        /// <param name="cmpExchangeKey"></param>
        /// <typeparam name="TValue">Value type.</typeparam>
        /// <returns>Compare exchange value if exists.</returns>
        public Task<CompareExchangeValue<TValue>?> LoadCompareExchangeValueAsync<TValue>(string cmpExchangeKey)
        {
            if (string.IsNullOrWhiteSpace(cmpExchangeKey))
            {
                throw new ArgumentException(
                    $"Unexpected empty value for {nameof(cmpExchangeKey)} in {nameof(LoadCompareExchangeValueAsync)}");
            }

            IDocumentStore store = _documentSession.Advanced.DocumentStore;
            return store.Operations.SendAsync(
                new GetCompareExchangeValueOperation<TValue>(cmpExchangeKey)
            );
        }

        /// <summary>
        /// Update existing compare exchange value.
        /// </summary>
        /// <param name="existing">Existing compare exchange value.</param>
        /// <typeparam name="T">Compare exchange value type.</typeparam>
        /// <returns>Update result.</returns>
        public async Task<CompareExchangeResult<T>> UpdateCompareExchangeValueAsync<T>(CompareExchangeValue<T> existing)
        {
            if (existing == null)
            {
                throw new ArgumentNullException(nameof(existing));
            }

            CompareExchangeResult<T>? result = await _documentSession.Advanced.DocumentStore.Operations.SendAsync(
                new PutCompareExchangeValueOperation<T>(
                    existing.Key,
                    existing.Value,
                    existing.Index,
                    existing.Metadata
                ));
            return result;
        }
    }
}