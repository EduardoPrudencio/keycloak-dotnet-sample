using KeycloakAdapter;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

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
        public Task<IActionResult> Post()
        {
            //var userToAccess = new AccessUser
            //{
            //    createdTimestamp = ((DateTimeOffset)DateTime.Now).ToUnixTimeSeconds(),
            //    username = "Aguiar",
            //    enabled = true,
            //    totp = true,
            //    emailVerified = true,
            //    firstName = "Aguiar",
            //    lastName = "Silva",
            //    email = "qqc@teste.com",
            //    disableableCredentialTypes = new string[] { },
            //    requiredActions = new string[] { },
            //    notBefore = 0,
            //    access = new Access { manageGroupMembership = true, view = true, mapRoles = true, impersonate = true, manage = true },
            //    realmRoles = new string[] { "master" }
            //};

            //string userSerialized = JsonConvert.SerializeObject(userToAccess);


            string newUser = "{ \"firstName\":\"Mallor\",\"lastName\":\"Kargopolov\", \"email\":\"test2@test.com\", \"enabled\":\"true\", \"username\":\"Mallor\"}";

            //StatusCodeResult statusCode = default;

            //string url = "http://localhost:8080/auth/admin/realms/master/users";
            //string answer = string.Empty;
            StringContent httpConent = new StringContent(newUser, Encoding.UTF8, "application/json");
            string jwt = Request.Headers["Authorization"];

            var t = accessKeycloakData.TryCreateUser(jwt, httpConent);


            //try
            //{
            //    using (var httpClient = new HttpClient())
            //    {
            //        httpClient.DefaultRequestHeaders.Add("Authorization", jwt);
            //        HttpResponseMessage response = await httpClient.PostAsync(url, httpConent);
            //        statusCode = new StatusCodeResult((int)response.StatusCode);

            //        answer = await response.Content.ReadAsStringAsync();

            //        //OpenIdConnect openIdConnect = JsonConvert.DeserializeObject<OpenIdConnect>(answer);

            //        //if (openIdConnect.HasError) answer = openIdConnect.error_description;

            //    }
            //}
            //catch (Exception exp)
            //{ }

            return t;
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
