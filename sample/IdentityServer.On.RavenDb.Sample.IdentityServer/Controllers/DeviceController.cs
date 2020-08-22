using System.Threading.Tasks;
using IdentityServer4.Models;
using IdentityServer4.Services;
using Mcrio.IdentityServer.On.RavenDb.Sample.IdentityServer.Controllers.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Mcrio.IdentityServer.On.RavenDb.Sample.IdentityServer.Controllers
{
    [Authorize]
    public class DeviceController : Controller
    {
        private readonly IDeviceFlowInteractionService _deviceFlowInteractionService;

        public DeviceController(IDeviceFlowInteractionService deviceFlowInteractionService)
        {
            _deviceFlowInteractionService = deviceFlowInteractionService;
        }

        [HttpGet("/device")]
        public IActionResult Index()
        {
            return View(new DeviceCodeViewModel());
        }

        [HttpPost("/device/verify-user-code")]
        public async Task<IActionResult> VerifyCode(DeviceCodeViewModel viewModel)
        {
            DeviceFlowInteractionResult response = await _deviceFlowInteractionService
                .HandleRequestAsync(
                    viewModel.UserCode,
                    new ConsentResponse
                    {
                        ScopesValuesConsented = new[] { "my_api.access", "offline_access", "openid" }
                    }
                );
            return View();
        }
    }
}