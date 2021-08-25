using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace KeycloakAdapter
{
    public class KeycloakManager
    {
        private readonly string _baseAddress;
        private readonly string _urlAddRoleToUser;
        private readonly string _initialAccessAddress;
        private readonly string _clientId;
        private readonly string _clientSecret;
        private readonly string _userUrl;
        private readonly string _metadaAddressUrl;
        private readonly string _getUsersWithRoleName;
        private readonly Role[] _roles;

        public KeycloakManager(IConfiguration configuration)
        {
            string urlEnvironment = Environment.GetEnvironmentVariable("SECURITY_URL");
            string urlAddRoleToUser = Environment.GetEnvironmentVariable("ADDUSER_ROLE_URL");
            string urlMetaData = Environment.GetEnvironmentVariable("METADATA_URL");
            string urlSessionStart = Environment.GetEnvironmentVariable("SESSION_START_URL");
            string urlUser = Environment.GetEnvironmentVariable("USER_URL");
            string clientId = Environment.GetEnvironmentVariable("CLIENT_ID");
            string clientSecret = Environment.GetEnvironmentVariable("CLIENT_SECRET");
            string rolesConf = Environment.GetEnvironmentVariable("ROLES");

            _baseAddress = urlEnvironment ?? configuration["keycloakData:UrlBase"];
            _urlAddRoleToUser = _baseAddress + (urlAddRoleToUser ?? configuration["keycloakData:UrlAddRoleToUser"]);
            _metadaAddressUrl = _baseAddress + (urlMetaData ?? configuration["keycloakData:MetadataUrl"]);
            _initialAccessAddress = _baseAddress + (urlSessionStart ?? configuration["keycloakData:SessionStartUrl"]);
            _userUrl = _baseAddress + (urlUser ?? configuration["keycloakData:UserUrl"]);
            _getUsersWithRoleName = _baseAddress + configuration["keycloakData:UrlAddRoleToUser"];

            _clientId = clientId ?? configuration["keycloakData:ClientId"];
            _clientSecret = clientSecret ?? configuration["keycloakData:ClientSecret"];
            _roles = rolesConf != null ? System.Text.Json.JsonSerializer.Deserialize<Role[]>(rolesConf) : System.Text.Json.JsonSerializer.Deserialize<Role[]>(configuration["keycloakData:Roles"]);
        }

        public string InitialAccessAddress { get => _initialAccessAddress; }
        public string ClientId { get => _clientId; }
        public string ClientSecret { get => _clientSecret; }
        public string UserUrl { get => _userUrl; }
        public string MetadataUrl { get => _metadaAddressUrl; }

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

        public IEnumerable<KeyValuePair<string, string>> GetHeaderSessionStart(string refreshToken)
        {
            IEnumerable<KeyValuePair<string, string>> headerData = new List<KeyValuePair<string, string>>()
            {
                new KeyValuePair<string, string>("refresh_token", refreshToken),
                new KeyValuePair<string, string>("client_id", _clientId),
                new KeyValuePair<string, string>("grant_type", "refresh_token"),
                new KeyValuePair<string, string>("client_secret", _clientSecret),
            };

            return headerData;
        }

        public async Task<OpenIdConnect> TryLoginExecute(string login, string password)
        {
            using var httpClient = new HttpClient();
            using var content = new FormUrlEncodedContent(GetHeaderSessionStart(login, password));

            content.Headers.Clear();
            content.Headers.Add("Content-Type", "application/x-www-form-urlencoded");

            HttpResponseMessage response = await httpClient.PostAsync(_initialAccessAddress, content);
            string answer = await response.Content.ReadAsStringAsync();

            return GetAccessResult(answer);
        }

        public async Task<OpenIdConnect> TryLoginExecute(string refreshToken)
        {
            using var httpClient = new HttpClient();
            using var content = new FormUrlEncodedContent(GetHeaderSessionStart(refreshToken));

            content.Headers.Clear();
            content.Headers.Add("Content-Type", "application/x-www-form-urlencoded");

            HttpResponseMessage response = await httpClient.PostAsync(_initialAccessAddress, content);
            string answer = await response.Content.ReadAsStringAsync();

            return GetAccessResult(answer);
        }

        public async Task<int> TryCreateUser(string jwt, User user)
        {
            int statusCode = default;

            string newUser = CreateUserDataInsert(user);
            StringContent httpConent = new StringContent(newUser, Encoding.UTF8, "application/json");

            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("Authorization", jwt);
            HttpResponseMessage response = await httpClient.PostAsync(_userUrl, httpConent);
            statusCode = (int)response.StatusCode;

            string answer = await response.Content.ReadAsStringAsync();

            OpenIdConnect openIdConnect = JsonConvert.DeserializeObject<OpenIdConnect>(answer);

            if (openIdConnect != null && openIdConnect.HasError) answer = openIdConnect.error_description ?? openIdConnect.errorMessage;

            return statusCode;
        }

        public async Task<int> TryUpdateUser(string jwt, User user)
        {
            int statusCode = default;

            if (string.IsNullOrWhiteSpace(user.Id)) return 400;

            string newUser = CreateUserDataUpdate(user);
            StringContent httpConent = new StringContent(newUser, Encoding.UTF8, "application/json");

            string urlUpdateUser = $"{_userUrl}/{user.Id}";

            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("Authorization", jwt);
            HttpResponseMessage response = await httpClient.PutAsync(urlUpdateUser, httpConent);
            statusCode = (int)response.StatusCode;

            string answer = await response.Content.ReadAsStringAsync();

            OpenIdConnect openIdConnect = JsonConvert.DeserializeObject<OpenIdConnect>(answer);

            if (openIdConnect != null && openIdConnect.HasError) answer = openIdConnect.error_description ?? openIdConnect.errorMessage;

            return statusCode;
        }

        public async Task<int> TryDeleteUser(string jwt, User user)
        {
            int statusCode = default;

            if (string.IsNullOrWhiteSpace(user.Id)) return 400;

            string urlDeleteUser = $"{_userUrl}/{user.Id}";

            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("Authorization", jwt);
            HttpResponseMessage response = await httpClient.DeleteAsync(urlDeleteUser);
            statusCode = (int)response.StatusCode;

            string answer = await response.Content.ReadAsStringAsync();

            OpenIdConnect openIdConnect = JsonConvert.DeserializeObject<OpenIdConnect>(answer);

            if (openIdConnect != null && openIdConnect.HasError) answer = openIdConnect.error_description ?? openIdConnect.errorMessage;

            return statusCode;
        }

        public async Task<int> TryAddRole(string jwt, User user, string roleName)
        {
            Role roleToAdd = this._roles.FirstOrDefault(r => r.name.Equals(roleName));

            int statusCode = default;
            var _roles = new Role[] { roleToAdd };
            string listOfRole = JsonConvert.SerializeObject(_roles);

            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("Authorization", jwt);

            string url = _urlAddRoleToUser
                .Replace("[USER_UUID]", user.Id)
                .Replace("[CLIENT_UUID]", roleToAdd.containerId);

            var httpContent = new StringContent(listOfRole, Encoding.UTF8, "application/json");

            HttpResponseMessage response = await httpClient.PostAsync(url, httpContent);
            statusCode = (int)response.StatusCode;

            string answer = await response.Content.ReadAsStringAsync();

            OpenIdConnect openIdConnect = JsonConvert.DeserializeObject<OpenIdConnect>(answer);

            if (openIdConnect != null && openIdConnect.HasError) answer = openIdConnect.error_description ?? openIdConnect.errorMessage;

            return statusCode;
        }

        public async Task<HttpResponseObject<User>> FindUserByEmail(string jwt, string email)
        {
            HttpResponseObject<User> responseSearch = new HttpResponseObject<User>();
            List<User> userResponse;

            using var httpClient = new HttpClient();
            string url = $"{_userUrl}?email={email}";

            httpClient.DefaultRequestHeaders.Add("Authorization", jwt);
            HttpResponseMessage response = await httpClient.GetAsync(url);

            int statusCode = (int)response.StatusCode;

            string answer = await response.Content.ReadAsStringAsync();

            userResponse = JsonConvert.DeserializeObject<List<User>>(answer);

            User finalResponse = (userResponse.Any()) ? userResponse.FirstOrDefault(u => !string.IsNullOrEmpty(u.email)) : null;

            responseSearch.Create(statusCode, finalResponse);

            return responseSearch;
        }

        public async Task<HttpResponseObject<User>> FindUserById(string jwt, string id)
        {
            HttpResponseObject<User> responseSearch = new HttpResponseObject<User>();
            User userResponse;

            using var httpClient = new HttpClient();
            string url = $"{_userUrl}/{id}";

            httpClient.DefaultRequestHeaders.Add("Authorization", jwt);
            HttpResponseMessage response = await httpClient.GetAsync(url);

            int statusCode = (int)response.StatusCode;

            string answer = await response.Content.ReadAsStringAsync();

            userResponse = JsonConvert.DeserializeObject<User>(answer);

            responseSearch.Create(statusCode, userResponse);

            return responseSearch;
        }

        public async Task<int> ResetPassword(string jwt, string email, string password)
        {
            using var httpClient = new HttpClient();
            string url = $"{_userUrl}?email={email}";

            httpClient.DefaultRequestHeaders.Add("Authorization", jwt);

            using var response = await httpClient.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                string answer = await response.Content.ReadAsStringAsync();

                var userResponse = JsonConvert.DeserializeObject<List<User>>(answer);

                var finalResponse = (userResponse.Any()) ? userResponse.FirstOrDefault(u => !string.IsNullOrEmpty(u.email)) : null;
                if (finalResponse != null)
                {
                    var credentials = new Credentials
                    {
                        Value = password,
                        Type = "password",
                        Temporary = false
                    };
                    url = $"{_userUrl}/{finalResponse.Id}/reset-password";

                    StringContent httpContent = new StringContent(JsonConvert.SerializeObject(credentials), Encoding.UTF8, "application/json");
                    var result = await httpClient.PutAsync(url, httpContent);

                    return (int)result.StatusCode;
                }
                else
                    return 400;
            }
            return (int)response.StatusCode;
        }
        public async Task<HttpResponseObject<List<User>>> GetUsersByClientAndRoleName(string jwt, string roleName, int? first = null, int? max = null)
        {
            HttpResponseObject<List<User>> responseSearch = new HttpResponseObject<List<User>>();
            List<User> userResponse;
            var queryParams = new Dictionary<string, object>
            {
                [nameof(first)] = first,
                [nameof(max)] = max
            };

            using var httpClient = new HttpClient();
            string url = _getUsersWithRoleName.Replace("[role_name]", roleName);
            httpClient.DefaultRequestHeaders.Add("Authorization", jwt);
            HttpResponseMessage response = await httpClient.GetAsync(url);

            int statusCode = (int)response.StatusCode;
            if (statusCode != 200)
                return responseSearch;
            string answer = await response.Content.ReadAsStringAsync();

            userResponse = JsonConvert.DeserializeObject<List<User>>(answer);

            List<User> finalResponse = userResponse.Any() ? userResponse.Where(u => !string.IsNullOrEmpty(u.email)).ToList() : null;

            responseSearch.Create(statusCode, finalResponse);

            return responseSearch;
        }

        public async Task<int> TryRemoveRole(string jwt, User user, string roleName)
        {
            Role roleToAdd = this._roles.FirstOrDefault(r => r.name.Equals(roleName));

            int statusCode = default;
            var _roles = new Role[] { roleToAdd };
            string listOfRole = JsonConvert.SerializeObject(_roles);

            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("Authorization", jwt);

            string url = _urlAddRoleToUser
                .Replace("[USER_UUID]", user.Id)
                .Replace("[CLIENT_UUID]", roleToAdd.containerId);

            StringContent httpConent = new StringContent(listOfRole, Encoding.UTF8, "application/json");

            HttpRequestMessage request = new HttpRequestMessage
            {
                Content = httpConent,
                Method = HttpMethod.Delete,
                RequestUri = new Uri(url, UriKind.Relative)
            };

            var response = await httpClient.SendAsync(request);

            statusCode = (int)response.StatusCode;

            string answer = await response.Content.ReadAsStringAsync();

            OpenIdConnect openIdConnect = JsonConvert.DeserializeObject<OpenIdConnect>(answer);

            if (openIdConnect != null && openIdConnect.HasError) answer = openIdConnect.error_description ?? openIdConnect.errorMessage;

            return statusCode;
        }

        public static OpenIdConnect GetAccessResult(string answer)
        {
            return JsonConvert.DeserializeObject<OpenIdConnect>(answer);
        }

        public static string CreateUserDataInsert(User user)
        {
            StringBuilder userJson = new StringBuilder();
            userJson.Append("{ \"firstName\":\"");
            userJson.Append(user.firstName);
            userJson.Append("\",\"lastName\":\"");
            userJson.Append(user.lastName);
            userJson.Append("\",\"email\":\"");
            userJson.Append(user.email);
            userJson.Append("\",\"enabled\":\"true\", \"emailVerified\":\"true\",\"username\":\"");
            userJson.Append(user.username);
            userJson.Append("\",\"credentials\": [{ \"type\": \"password\",\"value\":\"");
            userJson.Append(user.password);
            userJson.Append("\",\"temporary\": false}]}");

            return userJson.ToString();
        }

        public static string CreateUserDataUpdate(User user)
        {
            StringBuilder userJson = new StringBuilder();
            userJson.Append("{ \"firstName\":\"");
            userJson.Append(user.firstName);
            userJson.Append("\",\"lastName\":\"");
            userJson.Append(user.lastName);
            userJson.Append("\",\"email\":\"");
            userJson.Append(user.email);
            userJson.Append("\",\"enabled\":\"true\", \"emailVerified\":\"true\",\"username\":\"");
            userJson.Append(user.username);
            userJson.Append("\"}");

            return userJson.ToString();
        }
    }
}