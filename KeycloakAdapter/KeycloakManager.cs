using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http.Results;

namespace KeycloakAdapter
{
    public class KeycloakManager
    {
        private string _initialAccessAddress;
        private string _clientId;
        private string _clientSecret;
        private string _createUserUrl;

        public KeycloakManager(IConfiguration configutation)
        {
            _initialAccessAddress = configutation["keycloakData:SessionStartUrl"];
            _clientId = configutation["keycloakData:ClientId"];
            _clientSecret = configutation["keycloakData:ClientSecret"];
            _createUserUrl = configutation["keycloakData:CreateUserUrl"];
        }

        public string InitialAccessAddress { get => _initialAccessAddress; }
        public string ClientId { get => _clientId; }
        public string ClientSecret { get => _clientSecret; }
        public string CreateUserUrl { get => _createUserUrl; }

        public IEnumerable<KeyValuePair<string, string>> GetHeaderSessionStart(string login, string password)
        {
            IEnumerable<KeyValuePair<string, string>> headerData = new List<KeyValuePair<string, string>>()
            {
                new KeyValuePair<string, string>("username", login),
                new KeyValuePair<string, string>("password", password),
                new KeyValuePair<string, string>("client_id", _clientId),
                new KeyValuePair<string, string>("grant_type", "password"),
                new KeyValuePair<string, string>("client_secret", _clientSecret),
            };

            return headerData;
        }

        public async Task<OpenIdConnect> TryLoginExecute(string login, string password)
        {
            using (var httpClient = new HttpClient())
            {
                using (var content = new FormUrlEncodedContent(GetHeaderSessionStart(login, password)))
                {
                    content.Headers.Clear();
                    content.Headers.Add("Content-Type", "application/x-www-form-urlencoded");

                    HttpResponseMessage response = await httpClient.PostAsync(_initialAccessAddress, content);
                    string answer = await response.Content.ReadAsStringAsync();

                    return GetAccessResult(answer);
                }
            }
        }

        public async Task<IActionResult> TryCreateUser(string jwt, StringContent httpConent)
        {
            StatusCodeResult statusCode = default;

            try
            {
                using (var httpClient = new HttpClient())
                {
                    httpClient.DefaultRequestHeaders.Add("Authorization", jwt);
                    HttpResponseMessage response = await httpClient.PostAsync(_createUserUrl, httpConent);
                    statusCode = new StatusCodeResult(response.StatusCode, new HttpRequestMessage());

                    string answer = await response.Content.ReadAsStringAsync();

                    OpenIdConnect openIdConnect = JsonConvert.DeserializeObject<OpenIdConnect>(answer);

                    if (openIdConnect.HasError) answer = openIdConnect.error_description ?? openIdConnect.errorMessage;

                }
            }
            catch (Exception exp)
            { }

            return (IActionResult)statusCode;
        }

        public OpenIdConnect GetAccessResult(string answer)
        {
            return JsonConvert.DeserializeObject<OpenIdConnect>(answer);
        }

    }
}
