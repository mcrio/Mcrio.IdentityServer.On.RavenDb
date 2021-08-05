using System;
using System.Net.Http;
using System.Threading.Tasks;
using IdentityModel;
using IdentityModel.Client;

namespace IdentityServer.On.RavenDb.Sample.DeviceFlowClient
{
    public static class Program
    {
        public static async Task Main(string[] args)
        {
            Console.Title = "OAuth Device FLow";
            Console.WriteLine("Hello my friend. Let's demonstrate the oauth device flow :]");
            Console.WriteLine();

            // get discovery document
            var authClient = new HttpClient();
            DiscoveryDocumentResponse discovery = await authClient.GetDiscoveryDocumentAsync("https://localhost:5001");

            DeviceAuthorizationResponse requestDeviceAuthResponse = await authClient.RequestDeviceAuthorizationAsync(
                new DeviceAuthorizationRequest
                {
                    Address = discovery.DeviceAuthorizationEndpoint,
                    ClientId = "device_flow",
                    ClientSecret = "device_flow_secret",
                    Scope = "openid offline_access my_api.access",
                });

            if (requestDeviceAuthResponse.IsError)
            {
                Console.WriteLine($"Error requesting device auth: {requestDeviceAuthResponse.Error}");
                return;
            }

            Console.WriteLine(
                $"Got User Code `{requestDeviceAuthResponse.UserCode}` and Device code `{requestDeviceAuthResponse.DeviceCode}`");
            Console.WriteLine();
            Console.WriteLine($"Open following URL in browser: {requestDeviceAuthResponse.VerificationUriComplete}");
            Console.WriteLine();

            string accessToken;
            string refreshToken;
            string idToken;
            while (true)
            {
                Console.WriteLine("Checking for device token...");
                TokenResponse requestDeviceTokenResponse = await authClient.RequestDeviceTokenAsync(
                    new DeviceTokenRequest
                    {
                        Address = discovery.TokenEndpoint,
                        ClientId = "device_flow",
                        ClientSecret = "device_flow_secret",
                        DeviceCode = requestDeviceAuthResponse.DeviceCode,
                    }
                );

                if (requestDeviceTokenResponse.IsError)
                {
                    if (requestDeviceTokenResponse.Error == OidcConstants.TokenErrors.AuthorizationPending ||
                        requestDeviceTokenResponse.Error == OidcConstants.TokenErrors.SlowDown)
                    {
                        Console.WriteLine($"{requestDeviceAuthResponse.Error}...waiting.");
                        await Task.Delay(requestDeviceAuthResponse.Interval * 1000);
                    }
                    else
                    {
                        Console.WriteLine($"Error: {requestDeviceTokenResponse.Error}");
                        return;
                    }
                }
                else
                {
                    accessToken = requestDeviceTokenResponse.AccessToken;
                    refreshToken = requestDeviceTokenResponse.RefreshToken;
                    idToken = requestDeviceTokenResponse.IdentityToken;
                    break;
                }
            }

            Console.WriteLine("Hurray! Got the tokens...");
            Console.WriteLine();
            Console.WriteLine("Access Token:");
            Console.WriteLine(accessToken ?? "NONE RECEIVED");
            Console.WriteLine();
            Console.WriteLine("Refresh Token:");
            Console.WriteLine(refreshToken ?? "NONE RECEIVED");
            Console.WriteLine();
            Console.WriteLine("Identity Token:");
            Console.WriteLine(idToken ?? "NONE RECEIVED");
            Console.WriteLine();

            Console.WriteLine("Press enter to request secret data from MyApi:");
            Console.ReadLine();

            var myApiClient = new HttpClient();
            myApiClient.SetBearerToken(accessToken);

            Console.WriteLine("Requesting https://localhost:5011/just-authenticated");
            Console.WriteLine();

            HttpResponseMessage response = await myApiClient.GetAsync("https://localhost:5011/just-authenticated");
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"Error: {response.StatusCode}");
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