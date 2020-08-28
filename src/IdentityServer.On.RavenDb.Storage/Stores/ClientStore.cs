using System;
using System.Threading.Tasks;
using IdentityServer4.Stores;
using Mcrio.IdentityServer.On.RavenDb.Storage.Entities;
using Mcrio.IdentityServer.On.RavenDb.Storage.Mappers;
using Mcrio.IdentityServer.On.RavenDb.Storage.RavenDb;
using Raven.Client.Documents.Session;

namespace Mcrio.IdentityServer.On.RavenDb.Storage.Stores
{
    public class ClientStore : ClientStore<Client>
    {
        public ClientStore(
            IIdentityServerDocumentSessionWrapper identityServerDocumentSessionWrapper,
            IIdentityServerStoreMapper mapper)
            : base(identityServerDocumentSessionWrapper, mapper)
        {
        }
    }

    public abstract class ClientStore<TClientEntity> : IClientStore
        where TClientEntity : Client
    {
        protected ClientStore(
            IIdentityServerDocumentSessionWrapper identityServerDocumentSessionWrapper,
            IIdentityServerStoreMapper mapper)
        {
            DocumentSession = identityServerDocumentSessionWrapper.Session;
            Mapper = mapper;
        }

        protected IAsyncDocumentSession DocumentSession { get; }

        protected IIdentityServerStoreMapper Mapper { get; }

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