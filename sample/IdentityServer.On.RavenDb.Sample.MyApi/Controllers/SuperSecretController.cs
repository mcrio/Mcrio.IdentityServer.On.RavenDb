using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Mcrio.IdentityServer.On.RavenDb.Sample.MyApi.Controllers
{
    public class SuperSecretController : Controller
    {
        [HttpGet("/")]
        public string Index()
        {
            return "Hello my friend... No need to authenticate to read this. Try /super-secret";
        }
        
        [HttpGet("/super-secret")]
        [Authorize]
        public string SuperSecret()
        {
            return "Hi there from the super secret controller action.";
        }
    }
}