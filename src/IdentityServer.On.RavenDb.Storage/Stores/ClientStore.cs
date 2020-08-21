using System;
using System.Threading.Tasks;
using IdentityServer4.Models;
using IdentityServer4.Stores;
using Mcrio.IdentityServer.On.RavenDb.Storage.Mappers;
using Microsoft.Extensions.Logging;
using Raven.Client.Documents.Session;

namespace Mcrio.IdentityServer.On.RavenDb.Storage.Stores
{
    public class ClientStore : IClientStore
    {
        private readonly IAsyncDocumentSession _documentSession;
        private readonly IIdentityServerStoreMapper _mapper;
        private readonly ILogger<ClientStore> _logger;

        public ClientStore(
            IdentityServerDocumentSessionProvider identityServerDocumentSessionProvider,
            IIdentityServerStoreMapper mapper,
            ILogger<ClientStore> logger)
        {
            _documentSession = identityServerDocumentSessionProvider();
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<Client> FindClientByIdAsync(string clientId)
        {
            if (clientId == null)
            {
                throw new ArgumentNullException(nameof(clientId));
            }

            string documentId = _mapper.CreateEntityId<Entities.Client>(clientId);
            Entities.Client client = await _documentSession
                .LoadAsync<Entities.Client>(documentId)
                .ConfigureAwait(false);

            return client is null ? null! : _mapper.ToModel(client);
        }
    }
}