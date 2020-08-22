using Mcrio.AspNetCore.Identity.On.RavenDb;
using Mcrio.AspNetCore.Identity.On.RavenDb.Model.Role;
using Mcrio.AspNetCore.Identity.On.RavenDb.Model.User;
using Mcrio.IdentityServer.On.RavenDb.Storage;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Raven.Client.Documents;
using Raven.Client.Documents.Conventions;
using Raven.Client.Documents.Session;

namespace Mcrio.IdentityServer.On.RavenDb.Sample.IdentityServer
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            // RAVENDB Store
            string databaseName = Configuration.GetSection("RavenDbDatabase").Value;
            var documentStore = new DocumentStore
            {
                Urls = new[] { Configuration.GetSection("RavenDbUrl").Value },
                Database = databaseName,
                Conventions =
                {
                    FindCollectionName = type =>
                    {
                        if (IdentityServerRavenDbConventions.TryGetCollectionName(
                            type,
                            out string? identityServerCollectionName))
                        {
                            return identityServerCollectionName;
                        }

                        if (IdentityRavenDbConventions.TryGetCollectionName<RavenIdentityUser, RavenIdentityRole>(
                            type,
                            out string? identityCollectionName))
                        {
                            return identityCollectionName;
                        }

                        return DocumentConventions.DefaultGetCollectionName(type);
                    }
                }
            };
            documentStore.Initialize();
            documentStore.EnsureDatabaseExists(databaseName, true);

            // RAVENDB Register services
            services.AddSingleton<IDocumentStore>(documentStore);
            services.AddScoped<IAsyncDocumentSession>(serviceProvider => serviceProvider
                .GetService<IDocumentStore>()
                .OpenAsyncSession());

            // ASP Core Identity using RavenDB stores. Must be before identity server.
            services
                .AddIdentity<RavenIdentityUser, RavenIdentityRole>(config =>
                {
                    config.SignIn.RequireConfirmedEmail = false;
                    config.User.RequireUniqueEmail = false;
                    config.Password.RequireDigit = false;
                    config.Password.RequiredLength = 1;
                    config.Password.RequireLowercase = false;
                    config.Password.RequireUppercase = false;
                    config.Password.RequireNonAlphanumeric = false;
                    config.Password.RequiredUniqueChars = 1;
                })
                .AddRavenDbStores(serviceProvider => serviceProvider.GetRequiredService<IAsyncDocumentSession>)
                .AddDefaultTokenProviders();

            services.ConfigureApplicationCookie(config =>
            {
                config.Cookie.Name = "identity_server_cookie";
                config.LoginPath = "/login";
                config.LogoutPath = "/logout";
            });

            // IDENTITY SERVER
            services
                .AddIdentityServer()
                .AddRavenDbStores(
                    serviceProvider => serviceProvider.GetRequiredService<IAsyncDocumentSession>,
                    operationalStoreOptions => Configuration
                        .GetSection("OperationalStoreOptions")
                        .Bind(operationalStoreOptions),
                    addOperationalStore: true,
                    addConfigurationStore: true,
                    addConfigurationStoreCache: true
                )
                .AddAspNetIdentity<RavenIdentityUser>()
                .AddDeveloperSigningCredential();

            // Add MVC
            services
                .AddControllersWithViews()
                .AddRazorRuntimeCompilation();
            
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();

            app.UseAuthentication();
            
            app.UseAuthorization();
            
            app.UseIdentityServer();

            app.UseEndpoints(endpoints => { endpoints.MapDefaultControllerRoute(); });
        }
    }
}