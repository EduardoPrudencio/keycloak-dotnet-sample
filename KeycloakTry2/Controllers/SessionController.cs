using KeycloakTry2.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Net.Http;
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
        public async Task<string> Post(string login, string password)
        {
            string answer = string.Empty;
            string url = _configutation["Oidc:SessionStartUrl"];
            string clientId = _configutation["Oidc:ClientId"];
            string clientSecret = _configutation["Oidc:ClientSecret"];


            IEnumerable<KeyValuePair<string, string>> postData = new List<KeyValuePair<string, string>>()
            {
                new KeyValuePair<string, string>("username", login),
                new KeyValuePair<string, string>("password", password),
                new KeyValuePair<string, string>("client_id", clientId),
                new KeyValuePair<string, string>("grant_type", "password"),
                new KeyValuePair<string, string>("client_secret", clientSecret),
            };

            using (var httpClient = new HttpClient())
            {
                using (var content = new FormUrlEncodedContent(postData))
                {
                    content.Headers.Clear();
                    content.Headers.Add("Content-Type", "application/x-www-form-urlencoded");

                    HttpResponseMessage response = await httpClient.PostAsync(url, content);
                    answer = await response.Content.ReadAsStringAsync();

                    OpenIdConnect openIdConnect = JsonConvert.DeserializeObject<OpenIdConnect>(answer);

                    if (openIdConnect.HasError) answer = openIdConnect.error_description;
                }
            }

            return answer;
        }
    }
}
