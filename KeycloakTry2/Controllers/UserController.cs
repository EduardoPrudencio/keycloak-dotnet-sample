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

        // GET: api/<UserController>
        [HttpGet]
        public IEnumerable<string> Get()
        {
            return new string[] { "value1", "value2" };
        }

        // GET api/<UserController>/5
        [HttpGet("{id}")]
        public string Get(int id)
        {
            return "value";
        }

        // POST api/<UserController>
        [HttpPost]
        [Authorize(Roles = "administrator")]
        public IActionResult Post([FromBody] User accessUser)
        {
            StatusCodeResult result = default;
            string newUser = accessKeycloakData.CreateUserData(accessUser);
            StringContent httpConent = new StringContent(newUser, Encoding.UTF8, "application/json");
            string jwt = Request.Headers["Authorization"];
            int statusCodeResult = accessKeycloakData.TryCreateUser(jwt, httpConent).Result;
            result = new StatusCodeResult(statusCodeResult);
            return result;
        }

        // PUT api/<UserController>/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody] string value)
        {
        }

        // DELETE api/<UserController>/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }
    }
}
