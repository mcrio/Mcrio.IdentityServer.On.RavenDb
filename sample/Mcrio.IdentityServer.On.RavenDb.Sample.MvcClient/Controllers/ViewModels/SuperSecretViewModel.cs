namespace Mcrio.IdentityServer.On.RavenDb.Sample.MvcClient.Controllers.ViewModels
{
    public class SuperSecretViewModel
    {
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
        public string IdToken { get; set; }

        public string? Error { get; set; }

        public SuperSecretViewModel(string accessToken, string refreshToken, string idToken)
        {
            AccessToken = accessToken;
            RefreshToken = refreshToken;
            IdToken = idToken;
        }
    }
}