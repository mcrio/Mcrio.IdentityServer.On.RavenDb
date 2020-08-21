using System.ComponentModel.DataAnnotations;

namespace Mcrio.IdentityServer.On.RavenDb.Sample.IdentityServer.Controllers.ViewModels
{
    public class RegisterViewModel
    {
        [Required]
        public string Username { get; set; }

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; }
        
        [DataType(DataType.Password)]
        [Compare("Password")]
        public string ConfirmPassword { get; set; }

        public string? ReturnUrl { get; set; }
        
        public string? Error { get; set; }
    }
}