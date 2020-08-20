using System;
using System.Net.Http;
using System.Threading.Tasks;
using IdentityModel.Client;

namespace IdentityServer.On.RavenDb.Sample.ConsoleClient
{
    public static class Program
    {
        public static async Task Main(string[] args)
        {
            Console.WriteLine("Hello my friend. Let's go :]");
            Console.WriteLine();

            // try requesting non authenticated
            var myApiClient = new HttpClient();
            HttpResponseMessage nonAuthResponse =
                await myApiClient.GetAsync("https://localhost:5011/just-authenticated");
            if (nonAuthResponse.IsSuccessStatusCode)
            {
                Console.WriteLine("Error: we were expecting a forbidden status code.");
                return;
            }

            Console.WriteLine("Requesting https://localhost:5011/just-authenticated");
            Console.WriteLine();

            Console.WriteLine("We are not authenticated. Access forbidden.");
            Console.WriteLine($"Status code: {nonAuthResponse.StatusCode}");
            Console.WriteLine();


            // get discovery document
            var authClient = new HttpClient();
            DiscoveryDocumentResponse disco = await authClient.GetDiscoveryDocumentAsync("https://localhost:5001");

            // request token
            Console.WriteLine("Lets request an access token...");
            Console.WriteLine();

            TokenResponse tokenResponse = await authClient.RequestClientCredentialsTokenAsync(
                new ClientCredentialsTokenRequest
                {
                    Address = disco.TokenEndpoint,
                    ClientId = "machine_to_machine",
                    ClientSecret = "machine_to_machine_secret",
                    Scope = "my_api.access shared_scope"
                });

            if (tokenResponse.IsError)
            {
                Console.WriteLine($"Error getting the token! {tokenResponse.Error}");
                return;
            }

            Console.WriteLine("Got the token:");
            Console.WriteLine(tokenResponse.Json);
            Console.WriteLine();

            // request api endpoint
            myApiClient.SetBearerToken(tokenResponse.AccessToken);

            Console.WriteLine("Requesting https://localhost:5011/just-authenticated");
            Console.WriteLine();

            HttpResponseMessage response = await myApiClient.GetAsync("https://localhost:5011/just-authenticated");
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"Error: {response.StatusCode}");
                return;
            }
            else
            {
                string content = await response.Content.ReadAsStringAsync();
                Console.WriteLine("Hurray! We got the secret content:");
                Console.WriteLine($"`{content}`");
            }
        }
    }
}