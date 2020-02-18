using IdentityModel.Client;

namespace WMTLogger
{
    public class ApiClientOptions
    {
        public TokenClientOptions TokenClientOptions { get; set; }

        public string Scopes { get; set; }
    }
}