using KeycloakAdapter;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace KeycloakTry2.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        KeycloakManager accessKeycloakData;
        public UserController(IConfiguration configutation)
        {
            accessKeycloakData = new KeycloakManager(configutation);
        }

        [HttpGet]
        public IEnumerable<string> Get()
        {
            return new string[] { "value1", "value2" };
        }

        [HttpGet("{id}")]
        public string Get(int id)
        {
            return "value";
        }

        [HttpPost]
        [Authorize(Roles = "administrator")]
        public IActionResult Post([FromBody] User accessUser)
        {
            IActionResult result = default;
            string newUser = accessKeycloakData.CreateUserData(accessUser);
            StringContent httpConent = new StringContent(newUser, Encoding.UTF8, "application/json");
            string jwt = Request.Headers["Authorization"];

            int statusCodeResult = accessKeycloakData.TryCreateUser(jwt, httpConent).Result;

            if (statusCodeResult == 201)
            {
                HttpResponseObject<User> userCreated = accessKeycloakData.FindUserByEmail(jwt, accessUser.email).Result;
                result = Created(" ", userCreated.Object);
            }
            else
            {
                result = new StatusCodeResult(statusCodeResult);
            }


            return result;
        }

        [HttpPut("{id}")]
        public void Put(int id, [FromBody] string value)
        {
        }

        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }
    }
}
