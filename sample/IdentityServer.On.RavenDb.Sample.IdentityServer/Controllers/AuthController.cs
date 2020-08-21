using System.Linq;
using System.Threading.Tasks;
using Mcrio.AspNetCore.Identity.On.RavenDb.Model.User;
using Mcrio.IdentityServer.On.RavenDb.Sample.IdentityServer.Controllers.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SignInResult = Microsoft.AspNetCore.Identity.SignInResult;

namespace Mcrio.IdentityServer.On.RavenDb.Sample.IdentityServer.Controllers
{
    public class AuthController : Controller
    {
        private readonly SignInManager<RavenIdentityUser> _signInManager;
        private readonly UserManager<RavenIdentityUser> _userManager;

        public AuthController(
            SignInManager<RavenIdentityUser> signInManager,
            UserManager<RavenIdentityUser> userManager)
        {
            _signInManager = signInManager;
            _userManager = userManager;
        }

        [HttpGet("/login")]
        public IActionResult Login([FromQuery] string returnUrl)
        {
            var vm = new LoginViewModel
            {
                ReturnUrl = returnUrl,
            };
            return View(vm);
        }

        [HttpPost("/login")]
        public async Task<IActionResult> Login(LoginViewModel viewModel)
        {
            if (!ModelState.IsValid)
            {
                return View(viewModel);
            }

            SignInResult loginResult = await _signInManager
                .PasswordSignInAsync(viewModel.Username, viewModel.Password, false, false)
                .ConfigureAwait(false);

            if (loginResult.Succeeded)
            {
                return new RedirectResult(viewModel.ReturnUrl ?? "/");
            }

            if (loginResult.IsLockedOut)
            {
                viewModel.Error = "Error: Locked out";
            }
            else if (loginResult.IsNotAllowed)
            {
                viewModel.Error = "Error: Not allowed";
            }
            else if (loginResult.RequiresTwoFactor)
            {
                viewModel.Error = "Error: required 2 factor";
            }
            else
            {
                viewModel.Error = "Error logging in";
            }


            return View(viewModel);
        }

        [HttpGet("/register")]
        public IActionResult Register([FromQuery] string returnUrl)
        {
            var vm = new RegisterViewModel
            {
                ReturnUrl = returnUrl,
            };
            return View(vm);
        }

        [HttpPost("/register")]
        public async Task<IActionResult> Register(RegisterViewModel viewModel)
        {
            if (!ModelState.IsValid)
            {
                return View(viewModel);
            }

            var newUser = new RavenIdentityUser { UserName = viewModel.Username };
            IdentityResult registerResult = await _userManager.CreateAsync(
                newUser,
                viewModel.Password
            );

            if (registerResult.Succeeded)
            {
                await _signInManager.SignInAsync(newUser, false);
                return new RedirectResult(viewModel.ReturnUrl ?? "/");
            }

            viewModel.Error = string.Join(',', registerResult.Errors);

            return View(viewModel);
        }

        [HttpGet("/logout")]
        public async Task<IActionResult> Logout([FromQuery] string returnUrl)
        {
            await _signInManager.SignOutAsync();
            return Redirect(returnUrl ?? "/");
        }
    }
}