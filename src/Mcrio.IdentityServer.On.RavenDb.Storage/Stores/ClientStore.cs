using System;
using System.Threading.Tasks;
using IdentityServer4.Stores;
using Mcrio.IdentityServer.On.RavenDb.Storage.Entities;
using Mcrio.IdentityServer.On.RavenDb.Storage.Mappers;
using Mcrio.IdentityServer.On.RavenDb.Storage.RavenDb;
using Raven.Client.Documents.Session;

namespace Mcrio.IdentityServer.On.RavenDb.Storage.Stores
{
    /// <inheritdoc />
    public class ClientStore : ClientStore<Client>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ClientStore"/> class.
        /// </summary>
        /// <param name="identityServerDocumentSessionProvider"></param>
        /// <param name="mapper"></param>
        public ClientStore(
            IdentityServerDocumentSessionProvider identityServerDocumentSessionProvider,
            IIdentityServerStoreMapper mapper)
            : base(identityServerDocumentSessionProvider, mapper)
        {
        }
    }

    /// <inheritdoc />
    public abstract class ClientStore<TClientEntity> : IClientStore
        where TClientEntity : Client
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ClientStore{TClientEntity}"/> class.
        /// </summary>
        /// <param name="identityServerDocumentSessionProvider"></param>
        /// <param name="mapper"></param>
        protected ClientStore(
            IdentityServerDocumentSessionProvider identityServerDocumentSessionProvider,
            IIdentityServerStoreMapper mapper)
        {
            DocumentSession = identityServerDocumentSessionProvider();
            Mapper = mapper;
        }

        /// <summary>
        /// Gets the document session.
        /// </summary>
        protected IAsyncDocumentSession DocumentSession { get; }

        /// <summary>
        /// Gets the mapper.
        /// </summary>
        protected IIdentityServerStoreMapper Mapper { get; }

        /// <inheritdoc />
        public virtual async Task<IdentityServer4.Models.Client> FindClientByIdAsync(string clientId)
        {
            if (clientId == null)
            {
                throw new ArgumentNullException(nameof(clientId));
            }

            string documentId = Mapper.CreateEntityId<TClientEntity>(clientId);
            TClientEntity client = await DocumentSession
                .LoadAsync<TClientEntity>(documentId)
                .ConfigureAwait(false);

            return client is null ? null! : Mapper.ToModel<TClientEntity, IdentityServer4.Models.Client>(client);
        }
    }
}