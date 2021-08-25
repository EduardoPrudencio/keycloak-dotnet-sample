//using KeycloakAdapter;

using KeycloakAdapter;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System.Threading.Tasks;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace KeycloakTry2.Controllers
{
    [AllowAnonymous]
    [Route("api/[controller]")]
    [ApiController]
    public class SessionController : ControllerBase
    {
        IConfiguration _configutation;
        public SessionController(IConfiguration configutation)
        {
            _configutation = configutation;
        }

        [HttpPost]
        public Task<string> Post(string login, string password)
        {
            string answer = string.Empty;

            KeycloakManager accessKeycloakData = new KeycloakManager(_configutation);

            OpenIdConnect openIdConnect = accessKeycloakData.TryLoginExecute(login, password).Result;

            if (openIdConnect.HasError) answer = openIdConnect.error_description;

            else answer = JsonConvert.SerializeObject(openIdConnect);

            return Task.FromResult(answer);
        }

        [HttpPost("ByRefreshToken")]
        public Task<string> Post(string refreshToken)
        {
            string answer = string.Empty;

            KeycloakManager accessKeycloakData = new KeycloakManager(_configutation);

            OpenIdConnect openIdConnect = accessKeycloakData.TryLoginExecute(refreshToken).Result;

            if (openIdConnect.HasError) answer = openIdConnect.error_description;

            else answer = JsonConvert.SerializeObject(openIdConnect);

            return Task.FromResult(answer);
        }
    }
}
