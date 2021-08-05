using System;
using System.Threading;
using System.Threading.Tasks;
using IdentityServer4.Models;
using Mcrio.IdentityServer.On.RavenDb.Storage.Mappers;
using Mcrio.IdentityServer.On.RavenDb.Storage.RavenDb;
using Microsoft.Extensions.Logging;
using Raven.Client.Documents.Session;
using Raven.Client.Exceptions;

namespace Mcrio.IdentityServer.On.RavenDb.Storage.Stores
{
    /// <inheritdoc />
    public class ClientStoreExtension : ClientStoreExtension<Client, Entities.Client>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ClientStoreExtension"/> class.
        /// </summary>
        /// <param name="identityServerDocumentSessionProvider"></param>
        /// <param name="mapper"></param>
        /// <param name="logger"></param>
        public ClientStoreExtension(
            IdentityServerDocumentSessionProvider identityServerDocumentSessionProvider,
            IIdentityServerStoreMapper mapper,
            ILogger<ClientStoreExtension> logger)
            : base(identityServerDocumentSessionProvider, mapper, logger)
        {
        }
    }

    /// <inheritdoc />
    public abstract class ClientStoreExtension<TClientModel, TClientEntity> : IClientStoreExtension<TClientModel>
        where TClientModel : Client
        where TClientEntity : Entities.Client
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ClientStoreExtension{TClientModel, TClientEntity}"/> class.
        /// </summary>
        /// <param name="identityServerDocumentSessionProvider"></param>
        /// <param name="mapper"></param>
        /// <param name="logger"></param>
        protected ClientStoreExtension(
            IdentityServerDocumentSessionProvider identityServerDocumentSessionProvider,
            IIdentityServerStoreMapper mapper,
            ILogger<ClientStoreExtension<TClientModel, TClientEntity>> logger)
        {
            DocumentSession = identityServerDocumentSessionProvider();
            Mapper = mapper;
            Logger = logger;
        }

        /// <summary>
        /// Gets the document session.
        /// </summary>
        protected IAsyncDocumentSession DocumentSession { get; }

        /// <summary>
        /// Gets the mapper.
        /// </summary>
        protected IIdentityServerStoreMapper Mapper { get; }

        /// <summary>
        /// Gets the logger.
        /// </summary>
        protected ILogger<ClientStoreExtension<TClientModel, TClientEntity>> Logger { get; }

        /// <inheritdoc />
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
                    "Error creating client. ClientId {ClientId}; Entity ID {EntityId}",
                    client.ClientId,
                    entity.Id
                );
                return StoreResult.Failure(ErrorDescriber.ConcurrencyException);
            }
            catch (Exception ex)
            {
                Logger.LogError(
                    ex,
                    "Error creating client. ClientId {ClientId}; Entity ID {EntityId}",
                    client.ClientId,
                    entity.Id
                );
                return StoreResult.Failure(ErrorDescriber.GeneralError);
            }
        }

        /// <inheritdoc />
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
                    "Error updating client. Entity ID {EntityId}",
                    entityId
                );
                return StoreResult.Failure(ErrorDescriber.ConcurrencyException);
            }
            catch (Exception ex)
            {
                Logger.LogError(
                    ex,
                    "Error updating client. Entity ID {EntityId}",
                    entityId
                );
                return StoreResult.Failure(ErrorDescriber.GeneralError);
            }
        }

        /// <inheritdoc />
        public virtual async Task<StoreResult> DeleteAsync(
            string clientId,
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
                    "Error deleting client. Entity ID {EntityId}",
                    entityId
                );
                return StoreResult.Failure(ErrorDescriber.ConcurrencyException);
            }
            catch (Exception ex)
            {
                Logger.LogError(
                    ex,
                    "Error deleting client. Entity ID {EntityId}",
                    entityId
                );
                return StoreResult.Failure(ErrorDescriber.GeneralError);
            }
        }

        /// <summary>
        /// Check client entity required fields.
        /// </summary>
        /// <param name="clientEntity">Client entity.</param>
        /// <param name="errorMessage">Error message.</param>
        /// <returns>True if all fields are correct, False otherwise.</returns>
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