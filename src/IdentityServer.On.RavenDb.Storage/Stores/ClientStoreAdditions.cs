using System;
using System.Threading;
using System.Threading.Tasks;
using IdentityServer4.Models;
using Mcrio.IdentityServer.On.RavenDb.Storage.Mappers;
using Mcrio.IdentityServer.On.RavenDb.Storage.Stores.Additions;
using Microsoft.Extensions.Logging;
using Raven.Client.Documents.Session;
using Raven.Client.Exceptions;

namespace Mcrio.IdentityServer.On.RavenDb.Storage.Stores
{
    public class ClientStoreAdditions : ClientStoreAdditions<Client, Entities.Client>
    {
        public ClientStoreAdditions(
            IdentityServerDocumentSessionProvider identityServerDocumentSessionProvider,
            IIdentityServerStoreMapper mapper,
            ILogger<ClientStoreAdditions<Client, Entities.Client>> logger)
            : base(identityServerDocumentSessionProvider, mapper, logger)
        {
        }
    }

    public abstract class ClientStoreAdditions<TClientModel, TClientEntity> : IClientStoreAdditions<TClientModel>
        where TClientModel : Client
        where TClientEntity : Entities.Client
    {
        protected ClientStoreAdditions(
            IdentityServerDocumentSessionProvider identityServerDocumentSessionProvider,
            IIdentityServerStoreMapper mapper,
            ILogger<ClientStoreAdditions<TClientModel, TClientEntity>> logger)
        {
            DocumentSession = identityServerDocumentSessionProvider();
            Mapper = mapper;
            Logger = logger;
        }

        protected IAsyncDocumentSession DocumentSession { get; }

        protected IIdentityServerStoreMapper Mapper { get; }

        protected ILogger<ClientStoreAdditions<TClientModel, TClientEntity>> Logger { get; }

        public virtual async Task<StoreResult> CreateAsync(
            TClientModel client,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (client == null)
            {
                throw new ArgumentNullException(nameof(client));
            }

            TClientEntity entity = Mapper.ToEntity<TClientModel, TClientEntity>(client);

            if (!CheckRequiredFields(entity, out string errorMsg))
            {
                return StoreResult.Failure(errorMsg);
            }

            try
            {
                await DocumentSession
                    .StoreAsync(
                        entity,
                        string.Empty,
                        entity.Id,
                        cancellationToken
                    )
                    .ConfigureAwait(false);
                await DocumentSession.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                return StoreResult.Success();
            }
            catch (ConcurrencyException concurrencyException)
            {
                Logger.LogError(
                    concurrencyException,
                    "Error creating client. ClientId {0}; Entity ID {1}",
                    client.ClientId,
                    entity.Id
                );
                return StoreResult.Failure(ErrorDescriber.ConcurrencyException);
            }
            catch (Exception ex)
            {
                Logger.LogError(
                    ex,
                    "Error creating client. ClientId {0}; Entity ID {1}",
                    client.ClientId,
                    entity.Id
                );
                return StoreResult.Failure(ErrorDescriber.GeneralError);
            }
        }

        public virtual async Task<StoreResult> UpdateAsync(
            TClientModel client,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (client == null)
            {
                throw new ArgumentNullException(nameof(client));
            }

            TClientEntity updatedEntity = Mapper.ToEntity<TClientModel, TClientEntity>(client);

            if (!CheckRequiredFields(updatedEntity, out string errorMsg))
            {
                return StoreResult.Failure(errorMsg);
            }

            string entityId = updatedEntity.Id;
            TClientEntity entityInSession = await DocumentSession
                .LoadAsync<TClientEntity>(entityId, cancellationToken)
                .ConfigureAwait(false);

            if (entityInSession is null)
            {
                return StoreResult.Failure(string.Format(ErrorDescriber.EntityNotFound, entityId));
            }

            Mapper.Map(updatedEntity, entityInSession);

            try
            {
                string changeVector = DocumentSession.Advanced.GetChangeVectorFor(entityInSession);
                await DocumentSession
                    .StoreAsync(entityInSession, changeVector, entityInSession.Id, cancellationToken)
                    .ConfigureAwait(false);
                await DocumentSession.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                return StoreResult.Success();
            }
            catch (ConcurrencyException concurrencyException)
            {
                Logger.LogError(
                    concurrencyException,
                    "Error updating client. Entity ID {1}",
                    entityId
                );
                return StoreResult.Failure(ErrorDescriber.ConcurrencyException);
            }
            catch (Exception ex)
            {
                Logger.LogError(
                    ex,
                    "Error updating client. Entity ID {1}",
                    entityId
                );
                return StoreResult.Failure(ErrorDescriber.GeneralError);
            }
        }

        public virtual async Task<StoreResult> DeleteAsync(string clientId,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(clientId))
            {
                throw new ArgumentNullException(nameof(clientId));
            }

            string entityId = Mapper.CreateEntityId<TClientEntity>(clientId);

            TClientEntity entityInSession = await DocumentSession
                .LoadAsync<TClientEntity>(
                    entityId,
                    cancellationToken)
                .ConfigureAwait(false);

            if (entityInSession is null)
            {
                return StoreResult.Failure(string.Format(ErrorDescriber.EntityNotFound, entityId));
            }

            try
            {
                string changeVector = DocumentSession.Advanced.GetChangeVectorFor(entityInSession);
                DocumentSession.Delete(entityId, changeVector);
                await DocumentSession.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                return StoreResult.Success();
            }
            catch (ConcurrencyException concurrencyException)
            {
                Logger.LogError(
                    concurrencyException,
                    "Error deleting client. Entity ID {1}",
                    entityId
                );
                return StoreResult.Failure(ErrorDescriber.ConcurrencyException);
            }
            catch (Exception ex)
            {
                Logger.LogError(
                    ex,
                    "Error deleting client. Entity ID {1}",
                    entityId
                );
                return StoreResult.Failure(ErrorDescriber.GeneralError);
            }
        }

        protected virtual bool CheckRequiredFields(TClientEntity clientEntity, out string errorMessage)
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