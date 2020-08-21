using System.ComponentModel.DataAnnotations;

namespace Mcrio.IdentityServer.On.RavenDb.Sample.IdentityServer.Controllers.ViewModels
{
    public class LoginViewModel
    {
        [Required]
        public string Username { get; set; }

        [Required]
        public string Password { get; set; }

        public string? ReturnUrl { get; set; }
        
        public string? Error { get; set; }
    }
}