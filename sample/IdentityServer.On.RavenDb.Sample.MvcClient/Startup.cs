using System.Collections.Generic;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Mcrio.IdentityServer.On.RavenDb.Sample.MvcClient
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddAuthentication(config =>
                {
                    config.DefaultScheme = "cookie";
                    config.DefaultChallengeScheme = "oidc";
                })
                .AddCookie("cookie")
                .AddOpenIdConnect("oidc", config =>
                {
                    config.Authority = "https://localhost:5001";
                    config.ClientId = "mvc";
                    config.ClientSecret = "mvc_secret";
                    config.SaveTokens = true;
                    config.ResponseType = "code";

                    config.Scope.Clear();
                    config.Scope.Add("openid");
                    config.Scope.Add("my_api.access");
                    config.Scope.Add("shared_scope");
                    config.Scope.Add("offline_access");
                });

            services.AddHttpClient();
            
            services
                .AddControllersWithViews()
                .AddRazorRuntimeCompilation();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints => { endpoints.MapControllers(); });
        }
    }
}