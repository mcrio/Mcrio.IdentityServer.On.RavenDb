using System;
using System.Threading;
using System.Threading.Tasks;
using IdentityServer4.Models;
using Mcrio.IdentityServer.On.RavenDb.Storage.Mappers;
using Mcrio.IdentityServer.On.RavenDb.Storage.Stores.Advanced;
using Microsoft.Extensions.Logging;
using Raven.Client.Documents.Session;
using Raven.Client.Exceptions;

namespace Mcrio.IdentityServer.On.RavenDb.Storage.Stores
{
    internal class ClientStoreAdditions : IClientStoreAdditions
    {
        private readonly IAsyncDocumentSession _documentSession;
        private readonly IIdentityServerStoreMapper _mapper;
        private readonly ILogger<ClientStore> _logger;

        public ClientStoreAdditions(
            DocumentSessionProvider documentSessionProvider,
            IIdentityServerStoreMapper mapper,
            ILogger<ClientStore> logger)
        {
            _documentSession = documentSessionProvider();
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<StoreResult> CreateAsync(Client client, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (client == null)
            {
                throw new ArgumentNullException(nameof(client));
            }

            Entities.Client entity = _mapper.ToEntity(client);

            if (!CheckRequiredFields(entity, out string errorMsg))
            {
                return StoreResult.Failure(errorMsg);
            }

            try
            {
                await _documentSession
                    .StoreAsync(
                        entity,
                        string.Empty,
                        entity.Id,
                        cancellationToken
                    )
                    .ConfigureAwait(false);
                await _documentSession.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                return StoreResult.Success();
            }
            catch (ConcurrencyException concurrencyException)
            {
                _logger.LogError(
                    concurrencyException,
                    "Error creating client. ClientId {0}; Entity ID {1}",
                    client.ClientId,
                    entity.Id
                );
                return StoreResult.Failure(ErrorDescriber.ConcurrencyException);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error creating client. ClientId {0}; Entity ID {1}",
                    client.ClientId,
                    entity.Id
                );
                return StoreResult.Failure(ErrorDescriber.GeneralError);
            }
        }

        public async Task<StoreResult> UpdateAsync(Client client, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (client == null)
            {
                throw new ArgumentNullException(nameof(client));
            }

            Entities.Client updatedEntity = _mapper.ToEntity(client);

            if (!CheckRequiredFields(updatedEntity, out string errorMsg))
            {
                return StoreResult.Failure(errorMsg);
            }

            string entityId = updatedEntity.Id;
            Entities.Client entityInSession = await _documentSession
                .LoadAsync<Entities.Client>(entityId, cancellationToken)
                .ConfigureAwait(false);

            if (entityInSession is null)
            {
                return StoreResult.Failure(string.Format(ErrorDescriber.EntityNotFound, entityId));
            }

            _mapper.Map(updatedEntity, entityInSession);

            try
            {
                string changeVector = _documentSession.Advanced.GetChangeVectorFor(entityInSession);
                await _documentSession
                    .StoreAsync(entityInSession, changeVector, entityInSession.Id, cancellationToken)
                    .ConfigureAwait(false);
                await _documentSession.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                return StoreResult.Success();
            }
            catch (ConcurrencyException concurrencyException)
            {
                _logger.LogError(
                    concurrencyException,
                    "Error updating client. Entity ID {1}",
                    entityId
                );
                return StoreResult.Failure(ErrorDescriber.ConcurrencyException);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error updating client. Entity ID {1}",
                    entityId
                );
                return StoreResult.Failure(ErrorDescriber.GeneralError);
            }
        }

        public async Task<StoreResult> DeleteAsync(string clientId, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(clientId))
            {
                throw new ArgumentNullException(nameof(clientId));
            }

            string entityId = _mapper.CreateEntityId<Entities.Client>(clientId);

            Entities.Client entityInSession = await _documentSession
                .LoadAsync<Entities.Client>(
                    entityId,
                    cancellationToken)
                .ConfigureAwait(false);

            if (entityInSession is null)
            {
                return StoreResult.Failure(string.Format(ErrorDescriber.EntityNotFound, entityId));
            }

            try
            {
                string changeVector = _documentSession.Advanced.GetChangeVectorFor(entityInSession);
                _documentSession.Delete(entityId, changeVector);
                await _documentSession.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                return StoreResult.Success();
            }
            catch (ConcurrencyException concurrencyException)
            {
                _logger.LogError(
                    concurrencyException,
                    "Error deleting client. Entity ID {1}",
                    entityId
                );
                return StoreResult.Failure(ErrorDescriber.ConcurrencyException);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error deleting client. Entity ID {1}",
                    entityId
                );
                return StoreResult.Failure(ErrorDescriber.GeneralError);
            }
        }

        private static bool CheckRequiredFields(Entities.Client clientEntity, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(clientEntity.ClientId))
            {
                errorMessage = ErrorDescriber.ClientIdMissing;
                return false;
            }

            if (string.IsNullOrWhiteSpace(clientEntity.ProtocolType))
            {
                errorMessage = ErrorDescriber.ProtocolTypeMissing;
                return false;
            }

            if (string.IsNullOrWhiteSpace(clientEntity.Id))
            {
                errorMessage = ErrorDescriber.IdMustNotBeEmpty;
                return false;
            }

            return true;
        }
    }
}