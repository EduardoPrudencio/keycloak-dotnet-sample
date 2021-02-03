namespace KeycloakAdapter
{
    public struct Role
    {
        public string id { get; set; }
        public string containerId { get; set; }
        public string name { get; set; }
        public bool composite { get; set; }
        public bool clientRole { get; set; }

    }
}
