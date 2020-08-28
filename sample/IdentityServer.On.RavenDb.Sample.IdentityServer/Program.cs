using System;
using IdentityServer4.Models;
using Mcrio.AspNetCore.Identity.On.RavenDb;
using Mcrio.AspNetCore.Identity.On.RavenDb.Model.User;
using Mcrio.IdentityServer.On.RavenDb.Storage;
using Mcrio.IdentityServer.On.RavenDb.Storage.RavenDb;
using Mcrio.IdentityServer.On.RavenDb.Storage.Stores;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Raven.Client.Documents;
using Raven.Client.Documents.Session;

namespace Mcrio.IdentityServer.On.RavenDb.Sample.IdentityServer
{
    public class Program
    {
        public static void Main(string[] args)
        {
            IHost host = CreateHostBuilder(args).Build();

            AddIdentityTestData(host);
            AddIdentityServerTestData(host);

            host.Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder => { webBuilder.UseStartup<Startup>(); });

        private static void AddIdentityTestData(IHost host)
        {
            using IServiceScope scope = host.Services.CreateScope();

            IAsyncDocumentSession identityDocumentSession =
                scope.ServiceProvider.GetRequiredService<IIdentityServerDocumentSessionWrapper>().Session;

            UserManager<RavenIdentityUser> userManager =
                scope.ServiceProvider.GetRequiredService<UserManager<RavenIdentityUser>>();

            try
            {
                userManager.CreateAsync(
                    new RavenIdentityUser("bob"),
                    "Pass123$"
                ).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                //
            }
            
            try
            {
                userManager.CreateAsync(
                    new RavenIdentityUser("alice"),
                    "Pass123$"
                ).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                //
            }
        }

        private static void AddIdentityServerTestData(IHost host)
        {
            using IServiceScope scope = host.Services.CreateScope();

            IResourceStoreExtension<IdentityResource, ApiResource, ApiScope> apiResourceStoreExtension =
                scope.ServiceProvider.GetRequiredService<IResourceStoreExtension<IdentityResource, ApiResource, ApiScope>>();
            IClientStoreExtension<Client> clientStoreExtension =
                scope.ServiceProvider.GetRequiredService<IClientStoreExtension<Client>>();

            IAsyncDocumentSession identityServerDocumentSession =
                scope.ServiceProvider.GetRequiredService<IIdentityServerDocumentSessionWrapper>().Session;

            bool hasApiResources = identityServerDocumentSession
                .Query<Mcrio.IdentityServer.On.RavenDb.Storage.Entities.ApiResource>()
                .AnyAsync().GetAwaiter().GetResult();
            if (!hasApiResources)
            {
                foreach (ApiResource apiResource in TestData.GetApiResources())
                {
                    apiResourceStoreExtension.CreateApiResourceAsync(apiResource).GetAwaiter().GetResult();
                }
            }

            bool hasClients = identityServerDocumentSession
                .Query<Mcrio.IdentityServer.On.RavenDb.Storage.Entities.Client>()
                .AnyAsync().GetAwaiter().GetResult();
            if (!hasClients)
            {
                foreach (Client client in TestData.GetClients())
                {
                    clientStoreExtension.CreateAsync(client).GetAwaiter().GetResult();
                }
            }

            bool hasScopes = identityServerDocumentSession
                .Query<Mcrio.IdentityServer.On.RavenDb.Storage.Entities.ApiScope>()
                .AnyAsync().GetAwaiter().GetResult();
            if (!hasScopes)
            {
                foreach (ApiScope apiScope in TestData.GetScopes())
                {
                    apiResourceStoreExtension.CreateApiScopeAsync(apiScope).GetAwaiter().GetResult();
                }
            }

            bool hasIdentityResources = identityServerDocumentSession
                .Query<Mcrio.IdentityServer.On.RavenDb.Storage.Entities.IdentityResource>()
                .AnyAsync().GetAwaiter().GetResult();
            if (!hasIdentityResources)
            {
                foreach (IdentityResource identityResource in TestData.GetIdentityResources())
                {
                    apiResourceStoreExtension.CreateIdentityResourceAsync(identityResource).GetAwaiter().GetResult();
                }
            }
        }
    }
}