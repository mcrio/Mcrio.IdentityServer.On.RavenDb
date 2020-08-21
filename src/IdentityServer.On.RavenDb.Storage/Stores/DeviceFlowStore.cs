using System;
using System.Threading.Tasks;
using IdentityModel;
using IdentityServer4.Models;
using IdentityServer4.Stores;
using IdentityServer4.Stores.Serialization;
using Mcrio.IdentityServer.On.RavenDb.Storage.Entities;
using Mcrio.IdentityServer.On.RavenDb.Storage.Mappers;
using Mcrio.IdentityServer.On.RavenDb.Storage.Stores.Advanced;
using Mcrio.IdentityServer.On.RavenDb.Storage.Stores.Exceptions;
using Mcrio.IdentityServer.On.RavenDb.Storage.Stores.Extensions;
using Microsoft.Extensions.Logging;
using Raven.Client.Documents.Operations.CompareExchange;
using Raven.Client.Documents.Session;
using ConcurrencyException = Raven.Client.Exceptions.ConcurrencyException;

namespace Mcrio.IdentityServer.On.RavenDb.Storage.Stores
{
    public class DeviceFlowStore : IDeviceFlowStore
    {
        private readonly IAsyncDocumentSession _documentSession;
        private readonly IPersistentGrantSerializer _serializer;
        private readonly IIdentityServerStoreMapper _mapper;
        private readonly ILogger<DeviceFlowStore> _logger;

        public DeviceFlowStore(
            IPersistentGrantSerializer serializer,
            IdentityServerDocumentSessionProvider identityServerDocumentSessionProvider,
            IIdentityServerStoreMapper mapper,
            ILogger<DeviceFlowStore> logger)
        {
            _documentSession = identityServerDocumentSessionProvider();
            _serializer = serializer;
            _mapper = mapper;
            _logger = logger;
        }

        /// <summary>
        /// Device code will be reserved through the RavenDb compare exchange, while the User code is part of the ID.
        /// </summary>
        /// <param name="deviceCode"></param>
        /// <param name="userCode"></param>
        /// <param name="data"></param>
        /// <returns>The <see cref="Task"/> that represents the asynchronous operation.</returns>
        /// <exception cref="DuplicateException">When concurrency exception.</exception>
        public async Task StoreDeviceAuthorizationAsync(string deviceCode, string userCode, DeviceCode data)
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

            DeviceFlowCode? deviceFlowCodeEntity = ToEntity(data, deviceCode, userCode);
            if (deviceFlowCodeEntity is null)
            {
                throw new Exception("Device flow code entity must not be null.");
            }

            if (!CheckRequiredFields(deviceFlowCodeEntity, out string errorMessage))
            {
                throw new ArgumentException(errorMessage);
            }

            // Reserve the unique DeviceCode.
            bool deviceCodeReservationResult = await _documentSession.CreateReservationAsync(
                RavenDbCompareExchangeExtension.ReservationType.DeviceCode,
                deviceCode,
                deviceFlowCodeEntity.Id
            ).ConfigureAwait(false);
            if (!deviceCodeReservationResult)
            {
                throw new DuplicateException();
            }

            var saveSuccess = false;
            try
            {
                await _documentSession
                    .StoreAsync(
                        deviceFlowCodeEntity,
                        string.Empty,
                        deviceFlowCodeEntity.Id
                    )
                    .ConfigureAwait(false);
                await _documentSession.SaveChangesAsync().ConfigureAwait(false);
                saveSuccess = true;
            }
            catch (ConcurrencyException)
            {
                throw new DuplicateException();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed storing new device flow code entity {}", ex.Message);
            }
            finally
            {
                if (!saveSuccess)
                {
                    bool removeResult = await _documentSession.RemoveReservationAsync(
                        RavenDbCompareExchangeExtension.ReservationType.DeviceCode,
                        deviceCode
                    ).ConfigureAwait(false);
                    if (!removeResult)
                    {
                        _logger.LogError(
                            $"Failed removing device code '{deviceCode}' from compare exchange "
                        );
                    }
                }
            }
        }

        /// <inheritdoc/>
        public async Task<DeviceCode> FindByUserCodeAsync(string userCode)
        {
            if (string.IsNullOrWhiteSpace(userCode))
            {
                throw new ArgumentNullException(nameof(userCode));
            }

            string entityId = CreateEntityId(userCode);

            DeviceFlowCode? deviceCodeFlow = await _documentSession
                .LoadAsync<DeviceFlowCode>(entityId)
                .ConfigureAwait(false);

            return ToModel(deviceCodeFlow?.Data)!;
        }

        /// <inheritdoc/>
        public async Task<DeviceCode> FindByDeviceCodeAsync(string deviceCode)
        {
            if (string.IsNullOrWhiteSpace(deviceCode))
            {
                throw new ArgumentNullException(nameof(deviceCode));
            }

            DeviceFlowCode? code = await FindDeviceFlowCodeAsync(deviceCode).ConfigureAwait(false);
            return ToModel(code?.Data)!;
        }

        /// <inheritdoc/>
        public async Task UpdateByUserCodeAsync(string userCode, DeviceCode data)
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

            DeviceFlowCode? existingEntity = await _documentSession
                .LoadAsync<DeviceFlowCode>(entityId)
                .ConfigureAwait(false);

            if (existingEntity is null)
            {
                _logger.LogError("Device code flow user code `{userCode}` not found in database", userCode);
                throw new InvalidOperationException("Could not update device code.");
            }

            DeviceFlowCode? newDataEntity = ToEntity(data, existingEntity.DeviceCode, userCode);
            string? newSubjectId = data.Subject?.FindFirst(JwtClaimTypes.Subject).Value;

            if (newSubjectId is null)
            {
                _logger.LogError(
                    "Device code flow update for user code `{userCode}` failed due to empty SubjectId",
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
                string changeVector = _documentSession.Advanced.GetChangeVectorFor(existingEntity);
                await _documentSession.StoreAsync(existingEntity, changeVector, existingEntity.Id);
                await _documentSession.SaveChangesAsync().ConfigureAwait(false);
            }
            catch (ConcurrencyException)
            {
                _logger.LogError(
                    "Failed updating device code flow for user code {} due to concurrency exception",
                    userCode
                );
                throw new Exceptions.ConcurrencyException();
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed updating device code flow for user code {} with message {}",
                    userCode,
                    ex.Message
                );
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task RemoveByDeviceCodeAsync(string deviceCode)
        {
            if (string.IsNullOrWhiteSpace(deviceCode))
            {
                throw new ArgumentNullException(deviceCode);
            }

            DeviceFlowCode? entity = await FindDeviceFlowCodeAsync(deviceCode).ConfigureAwait(false);
            if (entity is null)
            {
                _logger.LogDebug("Device flow code with code `{deviceCode}` not found", deviceCode);
                return;
            }

            var saveSuccess = false;
            try
            {
                string changeVector = _documentSession.Advanced.GetChangeVectorFor(entity);
                _documentSession.Delete(entity.Id, changeVector);
                await _documentSession.SaveChangesAsync().ConfigureAwait(false);
                saveSuccess = true;
            }
            catch (ConcurrencyException)
            {
                _logger.LogError(
                    "Failed removing device flow code for device code {} due to concurrency exception",
                    deviceCode
                );
                throw new Exceptions.ConcurrencyException();
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    $"Failed removing device flow code entity with device code `{deviceCode}`. {ex.Message}"
                );
            }
            finally
            {
                if (saveSuccess)
                {
                    bool removeResult = await _documentSession.RemoveReservationAsync(
                        RavenDbCompareExchangeExtension.ReservationType.DeviceCode,
                        deviceCode
                    ).ConfigureAwait(false);
                    if (!removeResult)
                    {
                        _logger.LogError(
                            $"Failed removing device flow code entity from compare exchange for device code`{deviceCode}` "
                        );
                    }
                }
            }
        }

        /// <summary>
        /// Checks if all required <see cref="DeviceFlowStore"/> properties are set.
        /// </summary>
        /// <param name="entity">The <see cref="DeviceFlowStore"/> object to check the requirements on.</param>
        /// <param name="errorMessage">Error message if there are missing properties.</param>
        /// <returns>True if the check succeeded, otherwise False.</returns>
        protected static bool CheckRequiredFields(DeviceFlowCode entity, out string errorMessage)
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
        /// Creates the <see cref="DeviceFlowCode"/> Id by using the user code.
        /// As the Device Code is supposed to be unique as well we will use the RavenDB compare exchange for the device code. 
        /// </summary>
        /// <param name="userCode">User code.</param>
        /// <returns><see cref="DeviceFlowCode"/> id represented by the collection prefix and the user code.</returns>
        protected string CreateEntityId(string userCode)
        {
            return _mapper.CreateEntityId<DeviceFlowCode>(userCode);
        }

        /// <summary>
        /// Converts a model to an entity.
        /// </summary>
        /// <param name="model"></param>
        /// <param name="deviceCode"></param>
        /// <param name="userCode"></param>
        /// <returns>Device code flow object.</returns>
        protected DeviceFlowCode? ToEntity(DeviceCode? model, string? deviceCode, string? userCode)
        {
            if (model == null || string.IsNullOrWhiteSpace(deviceCode) || string.IsNullOrWhiteSpace(userCode))
            {
                return null;
            }

            return new DeviceFlowCode
            {
                Id = CreateEntityId(userCode),
                DeviceCode = deviceCode,
                UserCode = userCode,
                ClientId = model.ClientId,
                SubjectId = model.Subject?.FindFirst(JwtClaimTypes.Subject).Value!,
                CreationTime = model.CreationTime,
                Expiration = model.CreationTime.AddSeconds(model.Lifetime),
                Data = _serializer.Serialize(model),
            };
        }

        /// <summary>
        /// Converts a serialized DeviceCode to a model.
        /// </summary>
        /// <param name="serializedDeviceCode">Serialized <see cref="DeviceCode"/> representation.</param>
        /// <returns>Deserialized <see cref="DeviceCode"/> model.</returns>
        protected DeviceCode? ToModel(string? serializedDeviceCode)
        {
            return serializedDeviceCode == null
                ? null
                : _serializer.Deserialize<DeviceCode>(serializedDeviceCode);
        }

        protected async Task<DeviceFlowCode?> FindDeviceFlowCodeAsync(string deviceCode)
        {
            CompareExchangeValue<string>? compareExchangeResult = await _documentSession.GetReservationAsync<string>(
                RavenDbCompareExchangeExtension.ReservationType.DeviceCode,
                deviceCode
            ).ConfigureAwait(false);

            if (compareExchangeResult is null)
            {
                return null;
            }

            string entityId = compareExchangeResult.Value;
            if (string.IsNullOrWhiteSpace(entityId))
            {
                return null;
            }

            DeviceFlowCode? code = await _documentSession
                .LoadAsync<DeviceFlowCode>(entityId)
                .ConfigureAwait(false);

            if (code is null)
            {
                _logger.LogWarning(
                    "Device code flow compare exchange has value but entity was not found. " +
                    $"Entity id: {entityId} DeviceCode: {deviceCode}"
                );
                return null;
            }

            if (code.DeviceCode != deviceCode)
            {
                _logger.LogWarning(
                    "Device code flow compare exchange value that points to a entity which device code value" +
                    $" differs from the compare exchange device code value. Entity id: {entityId} DeviceCode: {deviceCode}"
                );
                return null;
            }

            return code;
        }
    }
}