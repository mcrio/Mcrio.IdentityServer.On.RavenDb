using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Mcrio.IdentityServer.On.RavenDb.Sample.MyApi.Controllers
{
    public class SuperSecretController : Controller
    {
        [HttpGet("/")]
        public string Index()
        {
            return "Hello my friend... No need to authenticate to read this. Try /just-authenticated";
        }
        
        [HttpGet("/just-authenticated")]
        [Authorize]
        public string SuperSecret()
        {
            return "Hi there from MyApi and the super secret controller action.";
        }
    }
}