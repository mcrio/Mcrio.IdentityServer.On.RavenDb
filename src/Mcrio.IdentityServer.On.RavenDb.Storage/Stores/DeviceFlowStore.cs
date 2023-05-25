using System;
using System.Diagnostics;
using System.Threading.Tasks;
using IdentityModel;
using IdentityServer4.Models;
using IdentityServer4.Stores;
using IdentityServer4.Stores.Serialization;
using Mcrio.IdentityServer.On.RavenDb.Storage.Entities;
using Mcrio.IdentityServer.On.RavenDb.Storage.Mappers;
using Mcrio.IdentityServer.On.RavenDb.Storage.RavenDb;
using Mcrio.IdentityServer.On.RavenDb.Storage.Stores.Exceptions;
using Mcrio.IdentityServer.On.RavenDb.Storage.Stores.Extensions;
using Mcrio.IdentityServer.On.RavenDb.Storage.Stores.Utility;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Raven.Client.Documents.Operations.CompareExchange;
using Raven.Client.Documents.Session;
using ConcurrencyException = Raven.Client.Exceptions.ConcurrencyException;

namespace Mcrio.IdentityServer.On.RavenDb.Storage.Stores
{
    /// <inheritdoc />
    public class DeviceFlowStore : DeviceFlowStore<DeviceFlowCode>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DeviceFlowStore"/> class.
        /// </summary>
        /// <param name="serializer"></param>
        /// <param name="identityServerDocumentSessionProvider"></param>
        /// <param name="mapper"></param>
        /// <param name="logger"></param>
        /// <param name="operationalStoreOptions"></param>
        /// <param name="uniqueValuesReservationOptions"></param>
        public DeviceFlowStore(
            IPersistentGrantSerializer serializer,
            IdentityServerDocumentSessionProvider identityServerDocumentSessionProvider,
            IIdentityServerStoreMapper mapper,
            ILogger<DeviceFlowStore> logger,
            IOptionsSnapshot<OperationalStoreOptions> operationalStoreOptions,
            UniqueValuesReservationOptions uniqueValuesReservationOptions)
            : base(
                serializer,
                identityServerDocumentSessionProvider,
                mapper,
                logger,
                operationalStoreOptions,
                uniqueValuesReservationOptions)
        {
        }
    }

    /// <inheritdoc />
    public class DeviceFlowStore<TDeviceFlowCode> : DeviceFlowStore<TDeviceFlowCode, UniqueReservation>
        where TDeviceFlowCode : DeviceFlowCode, new()
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DeviceFlowStore{TDeviceFlowCode}"/> class.
        /// </summary>
        /// <param name="serializer"></param>
        /// <param name="identityServerDocumentSessionProvider"></param>
        /// <param name="mapper"></param>
        /// <param name="logger"></param>
        /// <param name="operationalStoreOptions"></param>
        /// <param name="uniqueValuesReservationOptions"></param>
        public DeviceFlowStore(
            IPersistentGrantSerializer serializer,
            IdentityServerDocumentSessionProvider identityServerDocumentSessionProvider,
            IIdentityServerStoreMapper mapper,
            ILogger<DeviceFlowStore<TDeviceFlowCode>> logger,
            IOptionsSnapshot<OperationalStoreOptions> operationalStoreOptions,
            UniqueValuesReservationOptions uniqueValuesReservationOptions)
            : base(
                serializer,
                identityServerDocumentSessionProvider,
                mapper,
                logger,
                operationalStoreOptions,
                uniqueValuesReservationOptions)
        {
        }

        /// <inheritdoc />
        protected override UniqueReservationDocumentUtility<UniqueReservation> CreateUniqueReservationDocumentsUtility(
            UniqueReservationType reservationType,
            string uniqueValue)
        {
            Debug.Assert(
                UniqueValuesReservationOptions.UseReservationDocumentsForUniqueValues,
                "Expected reservation documents to be configured for unique value reservations."
            );
            return new UniqueReservationDocumentUtility(
                DocumentSession,
                reservationType,
                uniqueValue
            );
        }
    }

    /// <inheritdoc />
    public abstract class DeviceFlowStore<TDeviceFlowCode, TUniqueReservation> : IDeviceFlowStore
        where TDeviceFlowCode : DeviceFlowCode, new()
        where TUniqueReservation : UniqueReservation
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DeviceFlowStore{TDeviceFlowCode,TUniqueReservation}"/> class.
        /// </summary>
        /// <param name="serializer"></param>
        /// <param name="identityServerDocumentSessionProvider"></param>
        /// <param name="mapper"></param>
        /// <param name="logger"></param>
        /// <param name="operationalStoreOptions"></param>
        /// <param name="uniqueValuesReservationOptions"></param>
        protected DeviceFlowStore(
            IPersistentGrantSerializer serializer,
            IdentityServerDocumentSessionProvider identityServerDocumentSessionProvider,
            IIdentityServerStoreMapper mapper,
            ILogger<DeviceFlowStore<TDeviceFlowCode, TUniqueReservation>> logger,
            IOptionsSnapshot<OperationalStoreOptions> operationalStoreOptions,
            UniqueValuesReservationOptions uniqueValuesReservationOptions)
        {
            DocumentSession = identityServerDocumentSessionProvider();
            Serializer = serializer;
            Mapper = mapper;
            Logger = logger;
            OperationalStoreOptions = operationalStoreOptions;
            UniqueValuesReservationOptions = uniqueValuesReservationOptions;
        }

        /// <summary>
        /// Gets the document session.
        /// </summary>
        protected IAsyncDocumentSession DocumentSession { get; }

        /// <summary>
        /// Gets the persisted grant serializer.
        /// </summary>
        protected IPersistentGrantSerializer Serializer { get; }

        /// <summary>
        /// Gets the mapper.
        /// </summary>
        protected IIdentityServerStoreMapper Mapper { get; }

        /// <summary>
        /// Gets the logger.
        /// </summary>
        protected ILogger<DeviceFlowStore<TDeviceFlowCode, TUniqueReservation>> Logger { get; }

        /// <summary>
        /// Gets the operational store options.
        /// </summary>
        protected IOptionsSnapshot<OperationalStoreOptions> OperationalStoreOptions { get; }

        /// <summary>
        /// Gets the configuration for unique values reservations.
        /// </summary>
        protected UniqueValuesReservationOptions UniqueValuesReservationOptions { get; }

        /// <summary>
        /// Device code will be reserved through the RavenDb compare exchange, while the User code is part of the ID.
        /// </summary>
        /// <param name="deviceCode"></param>
        /// <param name="userCode"></param>
        /// <param name="data"></param>
        /// <returns>The <see cref="Task"/> that represents the asynchronous operation.</returns>
        /// <exception cref="DuplicateException">When concurrency exception.</exception>
        public virtual Task StoreDeviceAuthorizationAsync(string deviceCode, string userCode, DeviceCode data)
        {
            if (string.IsNullOrWhiteSpace(deviceCode))
            {
                throw new ArgumentNullException(nameof(deviceCode));
            }

            if (string.IsNullOrWhiteSpace(userCode))
            {
                throw new ArgumentNullException(nameof(userCode));
            }

            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            TDeviceFlowCode? deviceFlowCodeEntity = ToEntity(data, deviceCode, userCode);
            if (deviceFlowCodeEntity is null)
            {
                throw new Exception("Device flow code entity must not be null.");
            }

            return StoreDeviceAuthorizationAsync(deviceFlowCodeEntity);
        }

        /// <inheritdoc/>
        public virtual async Task<DeviceCode> FindByUserCodeAsync(string userCode)
        {
            if (string.IsNullOrWhiteSpace(userCode))
            {
                throw new ArgumentNullException(nameof(userCode));
            }

            string entityId = CreateEntityId(userCode);

            TDeviceFlowCode? deviceCodeFlow = await DocumentSession
                .LoadAsync<TDeviceFlowCode>(entityId)
                .ConfigureAwait(false);

            return ToModel(deviceCodeFlow?.Data)!;
        }

        /// <inheritdoc/>
        public virtual async Task<DeviceCode> FindByDeviceCodeAsync(string deviceCode)
        {
            if (string.IsNullOrWhiteSpace(deviceCode))
            {
                throw new ArgumentNullException(nameof(deviceCode));
            }

            TDeviceFlowCode? code = await FindDeviceFlowCodeAsync(deviceCode).ConfigureAwait(false);
            return ToModel(code?.Data)!;
        }

        /// <inheritdoc/>
        public virtual async Task UpdateByUserCodeAsync(string userCode, DeviceCode data)
        {
            if (string.IsNullOrWhiteSpace(userCode))
            {
                throw new ArgumentNullException(nameof(userCode));
            }

            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            string entityId = CreateEntityId(userCode);

            TDeviceFlowCode? existingEntity = await DocumentSession
                .LoadAsync<TDeviceFlowCode>(entityId)
                .ConfigureAwait(false);

            if (existingEntity is null)
            {
                Logger.LogError(
                    "Device code flow user code `{UserCode}` not found in database",
                    userCode
                );
                throw new InvalidOperationException("Could not update device code.");
            }

            TDeviceFlowCode? newDataEntity = ToEntity(data, existingEntity.DeviceCode, userCode);
            string? newSubjectId = data.Subject?.FindFirst(JwtClaimTypes.Subject).Value;

            if (newSubjectId is null)
            {
                Logger.LogError(
                    "Device code flow update for user code `{UserCode}` failed due to empty SubjectId",
                    userCode
                );
                throw new ArgumentException("New subject id must not be null or empty.");
            }

            existingEntity.SubjectId = newSubjectId;
            existingEntity.Data = newDataEntity!.Data;

            if (!CheckRequiredFields(existingEntity, out string errorMessage))
            {
                throw new ArgumentException(errorMessage);
            }

            try
            {
                string changeVector = DocumentSession.Advanced.GetChangeVectorFor(existingEntity);
                await DocumentSession.StoreAsync(existingEntity, changeVector, existingEntity.Id);

                /*
                 * NOTE: According to the original IDS store implementation we are not modifying the expiration
                 * so no need to modify the expire flag (when enabled in options) for the compare exchange values
                 * or the reservation documents.
                 */

                await DocumentSession.SaveChangesAsync().ConfigureAwait(false);
            }
            catch (ConcurrencyException)
            {
                Logger.LogError(
                    "Failed updating device code flow for user code {UserCode} due to concurrency exception",
                    userCode
                );
                throw new Exceptions.ConcurrencyException();
            }
            catch (Exception ex)
            {
                Logger.LogError(
                    ex,
                    "Failed updating device code flow for user code {UserCode} with message {Message}",
                    userCode,
                    ex.Message
                );
                throw;
            }
        }

        /// <inheritdoc/>
        public virtual async Task RemoveByDeviceCodeAsync(string deviceCode)
        {
            if (string.IsNullOrWhiteSpace(deviceCode))
            {
                throw new ArgumentNullException(deviceCode);
            }

            TDeviceFlowCode? entity = await FindDeviceFlowCodeAsync(deviceCode).ConfigureAwait(false);
            if (entity is null)
            {
                Logger.LogDebug("Device flow code with code `{DeviceCode}` not found", deviceCode);
                return;
            }

            // cluster wide as we will deal with compare exchange values either directly or as atomic guards
            DocumentSession.Advanced.SetTransactionMode(TransactionMode.ClusterWide);
            DocumentSession.Advanced.UseOptimisticConcurrency = false; // cluster wide tx doesn't support opt. concurrency

            if (UniqueValuesReservationOptions.UseReservationDocumentsForUniqueValues)
            {
                UniqueReservationDocumentUtility<TUniqueReservation> uniqueReservationUtil =
                    CreateUniqueReservationDocumentsUtility(
                        UniqueReservationType.DeviceCode,
                        deviceCode
                    );
                await uniqueReservationUtil.MarkReservationForDeletionAsync();
            }
            else
            {
                // When using compare exchange for unique value reservations
                CompareExchangeUtility compareExchangeUtility = CreateCompareExchangeUtility();
                string compareExchangeKey = compareExchangeUtility.CreateCompareExchangeKey(
                    UniqueReservationType.DeviceCode,
                    deviceCode
                );
                CompareExchangeValue<string>? existingCompareExchange = await DocumentSession
                    .Advanced
                    .ClusterTransaction
                    .GetCompareExchangeValueAsync<string>(compareExchangeKey)
                    .ConfigureAwait(false);
                if (existingCompareExchange != null)
                {
                    DocumentSession.Advanced.ClusterTransaction.DeleteCompareExchangeValue(existingCompareExchange);
                }
            }

            try
            {
                // Cluster wide transaction, relying on atomic guards
                DocumentSession.Delete(entity.Id);
                await DocumentSession.SaveChangesAsync().ConfigureAwait(false);
            }
            catch (ConcurrencyException)
            {
                Logger.LogError(
                    "Failed removing device flow code for device code {DeviceCode} due to concurrency exception",
                    deviceCode
                );
                throw new Exceptions.ConcurrencyException();
            }
            catch (Exception ex)
            {
                Logger.LogError(
                    ex,
                    "Failed removing device flow code entity with device code `{DeviceCode}`. {Message}",
                    deviceCode,
                    ex.Message
                );
            }
        }

        /// <summary>
        /// Device code will be reserved through the RavenDb compare exchange, while the User code is part of the ID.
        /// </summary>
        /// <param name="deviceFlowCodeEntity">Device flow code entity.</param>
        /// <returns>The <see cref="Task"/> that represents the asynchronous operation.</returns>
        /// <exception cref="DuplicateException">When concurrency exception.</exception>
        protected virtual async Task StoreDeviceAuthorizationAsync(TDeviceFlowCode deviceFlowCodeEntity)
        {
            if (!CheckRequiredFields(deviceFlowCodeEntity, out string errorMessage))
            {
                throw new ArgumentException(errorMessage);
            }

            string deviceCode = deviceFlowCodeEntity.DeviceCode;

            // cluster wide as we will deal with compare exchange values either directly or as atomic guards
            // for unique reservations
            DocumentSession.Advanced.SetTransactionMode(TransactionMode.ClusterWide);
            DocumentSession.Advanced.UseOptimisticConcurrency = false; // cluster wide tx doesn't support opt. concurrency

            // optimistic concurrency handled by cluster wide transaction and atomic guards
            await DocumentSession.StoreAsync(deviceFlowCodeEntity).ConfigureAwait(false);

            DocumentSession.ManageDocumentExpiresMetadata(
                OperationalStoreOptions.Value,
                deviceFlowCodeEntity,
                deviceFlowCodeEntity.Expiration
            );

            // Handle unique device code
            if (UniqueValuesReservationOptions.UseReservationDocumentsForUniqueValues)
            {
                UniqueReservationDocumentUtility<TUniqueReservation> uniqueReservationUtil =
                    CreateUniqueReservationDocumentsUtility(
                        UniqueReservationType.DeviceCode,
                        deviceCode
                    );
                bool uniqueExists = await uniqueReservationUtil.CheckIfUniqueIsTakenAsync().ConfigureAwait(false);
                if (uniqueExists)
                {
                    throw new DuplicateException();
                }

                UniqueReservation reservationDocument = await uniqueReservationUtil
                    .CreateReservationDocumentAddToUnitOfWorkAsync(deviceFlowCodeEntity.Id)
                    .ConfigureAwait(false);

                // add document expiration to be the same as entity
                DocumentSession.ManageDocumentExpiresMetadata(
                    OperationalStoreOptions.Value,
                    reservationDocument,
                    deviceFlowCodeEntity.Expiration
                );
            }
            else
            {
                CompareExchangeUtility compareExchangeUtility = CreateCompareExchangeUtility();
                string compareExchangeKey = compareExchangeUtility.CreateCompareExchangeKey(
                    UniqueReservationType.DeviceCode,
                    deviceCode
                );
                CompareExchangeValue<string>? existingCompareExchange = await DocumentSession
                    .Advanced
                    .ClusterTransaction
                    .GetCompareExchangeValueAsync<string>(compareExchangeKey)
                    .ConfigureAwait(false);

                if (existingCompareExchange != null)
                {
                    throw new DuplicateException();
                }

                CompareExchangeValue<string> newCompareExchangeValue = DocumentSession
                    .Advanced
                    .ClusterTransaction
                    .CreateCompareExchangeValue(
                        compareExchangeKey,
                        deviceFlowCodeEntity.Id
                    );
                newCompareExchangeValue.ManageCompareExchangeExpiresMetadata(
                    OperationalStoreOptions.Value,
                    deviceFlowCodeEntity.Expiration
                );
            }

            try
            {
                await DocumentSession.SaveChangesAsync().ConfigureAwait(false);
            }
            catch (ConcurrencyException)
            {
                throw new DuplicateException();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed storing new device flow code entity {Message}", ex.Message);
            }
        }

        /// <summary>
        /// Checks if all required <see cref="DeviceFlowStore"/> properties are set.
        /// </summary>
        /// <param name="entity">The <see cref="DeviceFlowStore"/> object to check the requirements on.</param>
        /// <param name="errorMessage">Error message if there are missing properties.</param>
        /// <returns>True if the check succeeded, otherwise False.</returns>
        protected virtual bool CheckRequiredFields(TDeviceFlowCode entity, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(entity.UserCode))
            {
                errorMessage = ErrorDescriber.DeviceFlowCodeUserCodeMissing;
                return false;
            }

            if (string.IsNullOrWhiteSpace(entity.DeviceCode))
            {
                errorMessage = ErrorDescriber.DeviceFlowCodeDeviceCodeMissing;
                return false;
            }

            if (string.IsNullOrWhiteSpace(entity.ClientId))
            {
                errorMessage = ErrorDescriber.DeviceFlowCodeClientIdMissing;
                return false;
            }

            if (entity.CreationTime == default)
            {
                errorMessage = ErrorDescriber.DeviceFlowCodeCreationTimeMissing;
                return false;
            }

            if (entity.Expiration == default)
            {
                errorMessage = ErrorDescriber.DeviceFlowCodeExpirationMissing;
                return false;
            }

            if (string.IsNullOrWhiteSpace(entity.Data))
            {
                errorMessage = ErrorDescriber.DeviceFlowCodeDataMissing;
                return false;
            }

            if (string.IsNullOrWhiteSpace(entity.Id))
            {
                errorMessage = ErrorDescriber.IdMustNotBeEmpty;
                return false;
            }

            return true;
        }

        /// <summary>
        /// Creates the <see cref="TDeviceFlowCode"/> Id by using the user code.
        /// As the Device Code is supposed to be unique as well we will use the RavenDB compare exchange for the device code.
        /// </summary>
        /// <param name="userCode">User code.</param>
        /// <returns><see cref="TDeviceFlowCode"/> id represented by the collection prefix and the user code.</returns>
        protected virtual string CreateEntityId(string userCode)
        {
            return Mapper.CreateEntityId<TDeviceFlowCode>(userCode);
        }

        /// <summary>
        /// Converts a model to an entity.
        /// </summary>
        /// <param name="model"></param>
        /// <param name="deviceCode"></param>
        /// <param name="userCode"></param>
        /// <returns>Device code flow object.</returns>
        protected virtual TDeviceFlowCode? ToEntity(DeviceCode? model, string? deviceCode, string? userCode)
        {
            if (model == null || string.IsNullOrWhiteSpace(deviceCode) || string.IsNullOrWhiteSpace(userCode))
            {
                return null;
            }

            return new TDeviceFlowCode
            {
                Id = CreateEntityId(userCode),
                DeviceCode = deviceCode,
                UserCode = userCode,
                ClientId = model.ClientId,
                SubjectId = model.Subject?.FindFirst(JwtClaimTypes.Subject).Value!,
                CreationTime = model.CreationTime,
                Expiration = model.CreationTime.AddSeconds(model.Lifetime),
                Data = Serializer.Serialize(model),
            };
        }

        /// <summary>
        /// Converts a serialized DeviceCode to a model.
        /// </summary>
        /// <param name="serializedDeviceCode">Serialized <see cref="DeviceCode"/> representation.</param>
        /// <returns>Deserialized <see cref="DeviceCode"/> model.</returns>
        protected virtual DeviceCode? ToModel(string? serializedDeviceCode)
        {
            return serializedDeviceCode == null
                ? null
                : Serializer.Deserialize<DeviceCode>(serializedDeviceCode);
        }

        /// <summary>
        /// Find device flow code by given device code.
        /// </summary>
        /// <param name="deviceCode">Device code to lookup.</param>
        /// <returns>DeviceFlowCode if found, otherwise Null.</returns>
        protected virtual async Task<TDeviceFlowCode?> FindDeviceFlowCodeAsync(string deviceCode)
        {
            string? deviceFlowCodeId = null;
            if (UniqueValuesReservationOptions.UseReservationDocumentsForUniqueValues)
            {
                UniqueReservationDocumentUtility<TUniqueReservation> uniqueReservationUtil =
                    CreateUniqueReservationDocumentsUtility(
                        UniqueReservationType.DeviceCode,
                        deviceCode
                    );
                UniqueReservation? reservation =
                    await uniqueReservationUtil.LoadReservationAsync().ConfigureAwait(false);
                if (reservation != null)
                {
                    deviceFlowCodeId = reservation.ReferenceId;
                }
            }
            else
            {
                CompareExchangeUtility compareExchangeUtility = CreateCompareExchangeUtility();
                string compareExchangeKey = compareExchangeUtility.CreateCompareExchangeKey(
                    UniqueReservationType.DeviceCode,
                    deviceCode
                );
                CompareExchangeValue<string>? existingCompareExchange = await compareExchangeUtility
                    .LoadCompareExchangeValueAsync<string>(compareExchangeKey)
                    .ConfigureAwait(false);
                if (existingCompareExchange != null)
                {
                    deviceFlowCodeId = existingCompareExchange.Value;
                }
            }

            if (string.IsNullOrWhiteSpace(deviceFlowCodeId))
            {
                return null;
            }

            TDeviceFlowCode? code = await DocumentSession
                .LoadAsync<TDeviceFlowCode>(deviceFlowCodeId)
                .ConfigureAwait(false);

            if (code is null)
            {
                Logger.LogWarning(
                    "Device code flow compare exchange has value but entity was not found. " +
                    "Entity id: {EntityId} DeviceCode: {DeviceCode}",
                    deviceFlowCodeId,
                    deviceCode
                );
                return null;
            }

            if (code.DeviceCode != deviceCode)
            {
                Logger.LogWarning(
                    "Device code flow compare exchange value that points to a entity which device code value" +
                    " differs from the compare exchange device code value. Entity id: {EntityId} DeviceCode: {DeviceCode}",
                    deviceFlowCodeId,
                    deviceCode
                );
                return null;
            }

            return code;
        }

        /// <summary>
        /// Creates an instance of the <see cref="CompareExchangeUtility"/>.
        /// </summary>
        /// <returns>Instance of <see cref="CompareExchangeUtility"/>.</returns>
        protected virtual CompareExchangeUtility CreateCompareExchangeUtility()
        {
            Debug.Assert(
                !UniqueValuesReservationOptions.UseReservationDocumentsForUniqueValues,
                "Expected compare exchange values to be configured for unique value reservations."
            );
            return new CompareExchangeUtility(DocumentSession);
        }

        /// <summary>
        /// Create an instance of <see cref="UniqueReservationDocumentUtility"/>.
        /// </summary>
        /// <param name="reservationType"></param>
        /// <param name="uniqueValue"></param>
        /// <returns>Instance of <see cref="UniqueReservationDocumentUtility"/>.</returns>
        protected abstract UniqueReservationDocumentUtility<TUniqueReservation> CreateUniqueReservationDocumentsUtility(
            UniqueReservationType reservationType,
            string uniqueValue);
    }
}