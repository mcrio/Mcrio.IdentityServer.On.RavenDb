// using System;
// using System.Collections.Generic;
// using System.Linq;
// using System.Threading.Tasks;
// using Mcrio.AspNetCore.Identity.On.RavenDb.Model;
// using Raven.Client.Documents.Session;
//
// namespace Mcrio.AspNetCore.Identity.On.RavenDb.Stores.Extensions
// {
//     /// <summary>
//     /// Extension methods that handle unique value reservations for changed properties.
//     /// </summary>
//     internal static class StoreUniquePropertyChangeExtension
//     {
//         /// <summary>
//         /// If there is a property change that requires uniqueness check, make a new compare exchange
//         /// reservation or throw if unique value already exists.
//         /// </summary>
//         /// <param name="documentSession">Document session.</param>
//         /// <param name="entityId">Entity id.</param>
//         /// <param name="changedPropertyName">Name of property we are checking the change for.</param>
//         /// <param name="newUniqueValue">New unique value we want to reserve.</param>
//         /// <param name="cmpExchangeReservationType">Compare exchange reservation type.</param>
//         /// <returns>Optional property change data if there was a property change and
//         /// a successful new compare exchange reservation made.</returns>
//         /// <exception cref="UniqueValueExistsException">If new unique value already exists.</exception>
//         internal static async Task<PropertyChange<string>?> ReserveIfPropertyChangedAsync(
//             this IAsyncDocumentSession documentSession,
//             string entityId,
//             string changedPropertyName,
//             string newUniqueValue,
//             RavenDbCompareExchangeExtension.ReservationType cmpExchangeReservationType)
//         {
//             IDictionary<string, DocumentsChanges[]> whatChanged = documentSession.Advanced.WhatChanged();
//             if (whatChanged.ContainsKey(entityId))
//             {
//                 DocumentsChanges? change = whatChanged[entityId]
//                     .FirstOrDefault(changes =>
//                         changes.Change == DocumentsChanges.ChangeType.FieldChanged
//                         && changes.FieldName == changedPropertyName
//                     );
//                 if (change != null)
//                 {
//                     if (newUniqueValue != change.FieldNewValue.ToString())
//                     {
//                         throw new Exception(
//                             $"User updated {changedPropertyName} property '{newUniqueValue}' should match change "
//                             + $"trackers recorded new value '{change.FieldNewValue}'"
//                         );
//                     }
//
//                     bool reservedNewValue = await documentSession
//                         .CreateReservationAsync<string>(
//                             cmpExchangeReservationType,
//                             newUniqueValue
//                         ).ConfigureAwait(false);
//                     if (!reservedNewValue)
//                     {
//                         throw new UniqueValueExistsException($"Unique value {newUniqueValue} already exists.");
//                     }
//
//                     return new PropertyChange<string>(
//                         change.FieldOldValue.ToString(),
//                         newUniqueValue
//                     );
//                 }
//             }
//
//             return null;
//         }
//     }
// }