using Newtonsoft.Json;

namespace KeycloakAdapter
{
    public class Credentials
    {
        [JsonProperty("temporary")]
        public bool? Temporary { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("value")]
        public string Value { get; set; }
    }
}