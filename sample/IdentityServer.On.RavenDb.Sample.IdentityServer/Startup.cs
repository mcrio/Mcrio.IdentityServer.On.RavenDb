using System;
using IdentityServer4.Models;
using Mcrio.IdentityServer.On.RavenDb.Storage;
using Mcrio.IdentityServer.On.RavenDb.Storage.Stores.Advanced;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Raven.Client.Documents;
using Raven.Client.Documents.Conventions;
using Raven.Client.Documents.Operations;
using Raven.Client.Documents.Session;
using Raven.Client.Exceptions;
using Raven.Client.Exceptions.Database;
using Raven.Client.ServerWide;
using Raven.Client.ServerWide.Operations;

namespace Mcrio.IdentityServer.On.RavenDb.Sample.IdentityServer
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllersWithViews();

            // RAVENDB Store
            string databaseName = Configuration.GetSection("RavenDbDatabase").Value;
            var documentStore = new DocumentStore
            {
                Urls = new[] { Configuration.GetSection("RavenDbUrl").Value },
                Database = databaseName,
                Conventions =
                {
                    FindCollectionName = type => RavenDbConventions.GetIdentityServerCollectionName(type) ??
                                                 DocumentConventions.DefaultGetCollectionName(type)
                }
            };
            documentStore.Initialize();
            EnsureDatabaseExists(documentStore, databaseName, true);

            // RAVENDB Services
            services.AddSingleton<IDocumentStore>(documentStore);
            services.AddScoped<IAsyncDocumentSession>(serviceProvider => serviceProvider
                .GetService<IDocumentStore>()
                .OpenAsyncSession());

            // IDENTITY SERVER
            services.AddIdentityServer()
                .AddRavenDbStores(
                    serviceProvider => serviceProvider.GetRequiredService<IAsyncDocumentSession>,
                    tokenCleanupOptions => Configuration
                        .GetSection("IdentityServerTokenCleanup")
                        .Bind(tokenCleanupOptions),
                    addOperationalStore: true,
                    addConfigurationStore: true,
                    addConfigurationStoreCache: true
                )
                .AddDeveloperSigningCredential();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();

            app.UseIdentityServer();

            app.UseEndpoints(endpoints => { endpoints.MapDefaultControllerRoute(); });

            AddIdentityServerTestData(app);
        }

        private static void EnsureDatabaseExists(
            IDocumentStore store,
            string? database = null,
            bool createDatabaseIfNotExists = true)
        {
            database ??= store.Database;

            if (string.IsNullOrWhiteSpace(database))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(database));

            try
            {
                store.Maintenance.ForDatabase(database).Send(new GetStatisticsOperation());
            }
            catch (DatabaseDoesNotExistException)
            {
                if (createDatabaseIfNotExists == false)
                    throw;

                try
                {
                    store.Maintenance.Server.Send(new CreateDatabaseOperation(new DatabaseRecord(database)));
                }
                catch (ConcurrencyException)
                {
                    // The database was already created before calling CreateDatabaseOperation
                }
            }
        }

        private static void AddIdentityServerTestData(IApplicationBuilder app)
        {
            using IServiceScope scope = app.ApplicationServices.CreateScope();

            IResourceStoreAdditions apiResourceStoreAdditions = scope.ServiceProvider.GetRequiredService<IResourceStoreAdditions>();
            IClientStoreAdditions clientStoreAdditions = scope.ServiceProvider.GetRequiredService<IClientStoreAdditions>();

            IAsyncDocumentSession documentSession =
                scope.ServiceProvider.GetRequiredService<DocumentSessionProvider>()();

            bool hasApiResources = documentSession
                .Query<Mcrio.IdentityServer.On.RavenDb.Storage.Entities.ApiResource>()
                .AnyAsync().GetAwaiter().GetResult();
            if (!hasApiResources)
            {
                foreach (ApiResource apiResource in TestData.GetApiResources())
                {
                    apiResourceStoreAdditions.CreateApiResourceAsync(apiResource).GetAwaiter().GetResult();
                }
            }

            bool hasClients = documentSession
                .Query<Mcrio.IdentityServer.On.RavenDb.Storage.Entities.Client>()
                .AnyAsync().GetAwaiter().GetResult();
            if (!hasClients)
            {
                foreach (Client client in TestData.GetClients())
                {
                    clientStoreAdditions.CreateAsync(client).GetAwaiter().GetResult();
                }
            }
            
            bool hasScopes = documentSession
                .Query<Mcrio.IdentityServer.On.RavenDb.Storage.Entities.ApiScope>()
                .AnyAsync().GetAwaiter().GetResult();
            if (!hasScopes)
            {
                foreach (ApiScope apiScope in TestData.GetScopes())
                {
                    apiResourceStoreAdditions.CreateApiScopeAsync(apiScope).GetAwaiter().GetResult();
                }
            }
        }
    }
}