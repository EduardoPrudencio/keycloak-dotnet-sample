namespace KeycloakAdapter
{
    public class OpenIdConnect
    {
        public string access_token { get; set; }
        public int expires_in { get; set; }
        public int refresh_expires_in { get; set; }
        public string refresh_token { get; set; }
        public string token_type { get; set; }

        public int not_before_policy { get; set; }
        public string session_state { get; set; }
        public string scope { get; set; }
        public string error { get; set; }
        public string errorMessage { get; set; }

        public string error_description { get; set; }

        public bool HasError { get => !string.IsNullOrEmpty(error) || !string.IsNullOrEmpty(errorMessage); }

        public string[] roles { get; set; }

    }
}
