using System;
using System.Net.Http;
using System.Threading.Tasks;
using IdentityModel.Client;
using Newtonsoft.Json.Linq;

namespace IdentityServer.On.RavenDb.Sample.ConsoleClient
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            Console.WriteLine("Hello my friend. Let's go :]");
            Console.WriteLine("");
            var authClient = new HttpClient();
            DiscoveryDocumentResponse disco = await authClient.GetDiscoveryDocumentAsync("https://localhost:5001");

            // request token
            Console.WriteLine("Requesting token...");
            TokenResponse tokenResponse = await authClient.RequestClientCredentialsTokenAsync(
                new ClientCredentialsTokenRequest
                {
                    Address = disco.TokenEndpoint,
                    ClientId = "my_client",
                    ClientSecret = "my_secret",
                    Scope = "my_api"
                });

            if (tokenResponse.IsError)
            {
                Console.WriteLine($"Error getting the token! {tokenResponse.Error}");
                return;
            }

            Console.WriteLine($"Got the token: {tokenResponse.Json}");

            // request api endpoint
            var myApiClient = new HttpClient();
            myApiClient.SetBearerToken(tokenResponse.AccessToken);

            HttpResponseMessage response = await myApiClient.GetAsync("https://localhost:5011/super-secret");
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"Error: {response.StatusCode}");
            }
            else
            {
                string content = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Hurray! We got the content: {content}");
            }
        }
    }
}