using IdentityModel.Client;

namespace WMTDashboard
{
    public class ApiClientOptions
    {
        public TokenClientOptions TokenClientOptions { get; set; }

        public string Scopes { get; set; }
    }
}