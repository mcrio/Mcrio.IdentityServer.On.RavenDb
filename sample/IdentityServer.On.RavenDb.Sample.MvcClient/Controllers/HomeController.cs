using System;
using System.Net.Http;
using System.Threading.Tasks;
using IdentityModel.Client;
using Mcrio.IdentityServer.On.RavenDb.Sample.MvcClient.Controllers.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Mcrio.IdentityServer.On.RavenDb.Sample.MvcClient.Controllers
{
    public class HomeController : Controller
    {
        [HttpGet("/")]
        public IActionResult Index()
        {
            return View();
        }

        [HttpGet("/super-secret")]
        [Authorize]
        public async Task<IActionResult> SuperSecret(
            [FromServices] IHttpClientFactory httpClientFactory,
            [FromQuery] bool? refreshAccessToken
        )
        {
            string? accessToken = await HttpContext.GetTokenAsync("access_token");
            string? refreshToken = await HttpContext.GetTokenAsync("refresh_token");
            string? idToken = await HttpContext.GetTokenAsync("id_token");

            var viewModel = new SuperSecretViewModel(accessToken, refreshToken, idToken);

            if (refreshAccessToken.HasValue && refreshAccessToken.Value)
            {
                HttpClient authClient = httpClientFactory.CreateClient();
                DiscoveryDocumentResponse disco = await authClient.GetDiscoveryDocumentAsync("https://localhost:5001");

                TokenResponse tokenResponse = await authClient.RequestRefreshTokenAsync(
                    new RefreshTokenRequest()
                    {
                        Address = disco.TokenEndpoint,
                        RefreshToken = viewModel.RefreshToken,
                        ClientId = "mvc",
                        ClientSecret = "mvc_secret",
                    });

                if (tokenResponse.IsError)
                {
                    viewModel.Error = tokenResponse.Error;
                }

                AuthenticateResult auth = await HttpContext.AuthenticateAsync("cookie");
                auth.Properties.UpdateTokenValue("access_token", tokenResponse.AccessToken);
                auth.Properties.UpdateTokenValue("refresh_token", tokenResponse.RefreshToken);
                auth.Properties.UpdateTokenValue("id_token", tokenResponse.IdentityToken);

                await HttpContext.SignInAsync("cookie", auth.Principal, auth.Properties);

                viewModel.AccessToken = tokenResponse.AccessToken;
                viewModel.RefreshToken = tokenResponse.RefreshToken;
                viewModel.IdToken = tokenResponse.IdentityToken;
            }

            return View(viewModel);
        }

        [HttpGet("/call-api")]
        [Authorize]
        public async Task<IActionResult> CallApi([FromServices] IHttpClientFactory httpClientFactory)
        {
            HttpClient myApiClient = httpClientFactory.CreateClient();
            myApiClient.SetBearerToken(await HttpContext.GetTokenAsync("access_token"));

            var viewModel = new CallApiViewModel();

            HttpResponseMessage response = await myApiClient.GetAsync("https://localhost:5011/just-authenticated");
            if (!response.IsSuccessStatusCode)
            {
                viewModel.Error = $"Error with status code: {response.StatusCode}";
            }
            else
            {
                string content = await response.Content.ReadAsStringAsync();
                viewModel.ApiResponse = content;
            }

            return View(viewModel);
        }
    }
}