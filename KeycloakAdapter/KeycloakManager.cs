using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace KeycloakAdapter
{
    public class KeycloakManager
    {
        private string _initialAccessAddress;
        private string _clientId;
        private string _clientSecret;

        public KeycloakManager(IConfiguration configutation)
        {
            _initialAccessAddress = configutation["keycloakData:SessionStartUrl"];
            _clientId = configutation["keycloakData:ClientId"];
            _clientSecret = configutation["keycloakData:ClientSecret"];
        }

        public string InitialAccessAddress { get => _initialAccessAddress; }
        public string ClientId { get => _clientId; }
        public string ClientSecret { get => _clientSecret; }

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

                    //var openIdConnect = GetAccessResult(answer);

                    //if (openIdConnect.HasError) answer = openIdConnect.error_description;

                    return GetAccessResult(answer);
                }
            }
        }

        public OpenIdConnect GetAccessResult(string answer)
        {
            return JsonConvert.DeserializeObject<OpenIdConnect>(answer);
        }

    }
}
