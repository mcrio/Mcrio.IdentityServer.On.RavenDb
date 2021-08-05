using Microsoft.AspNetCore.Mvc;

namespace Mcrio.IdentityServer.On.RavenDb.Sample.IdentityServer.Controllers
{
    public class HomeController : Controller
    {
        [HttpGet("/")]
        public IActionResult Index()
        {
            return View();
        }
    }
}