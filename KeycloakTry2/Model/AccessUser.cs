namespace KeycloakTry2.Model
{
    public class AccessUser
    {
        public long createdTimestamp { get; set; }

        public string username { get; set; }
        public bool enabled { get; set; }
        public bool totp { get; set; }
        public bool emailVerified { get; set; }
        public string firstName { get; set; }
        public string lastName { get; set; }
        public string email { get; set; }
        public string[] disableableCredentialTypes { get; set; }
        public string[] requiredActions { get; set; }
        public int notBefore { get; set; }

        public Access access { get; set; }

        public string[] realmRoles { get; set; }

    }
}
