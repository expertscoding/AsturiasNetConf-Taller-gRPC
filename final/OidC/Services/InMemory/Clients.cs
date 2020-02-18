using System.Collections.Generic;
using System.Security.Claims;
using IdentityModel;
using IdentityServer4.Models;

namespace OidC.Services.InMemory
{
    public class Clients
    {
        public static IEnumerable<Client> Get()
        {
            return new List<Client>
            {
                new Client
                {
                    ClientId = "WMTServerDashboard",
                    ClientSecrets =
                    {
                        new Secret("Client_AsturiasNetConf2020".Sha256())
                    },
                    AllowedGrantTypes = GrantTypes.ClientCredentials,
                    Claims = new List<Claim>{new Claim(JwtClaimTypes.Role, "WMTServerAPI.reader")},
                    AllowedScopes = { "WMTServerAPI.full_access", "WMTServerAPI.read_only" }
                },
                new Client
                {
                    ClientId = "WMTBlazor",
                    ClientSecrets =
                    {
                        new Secret("Blzr_AsturiasNetConf2020".Sha256())
                    },
                    AllowedGrantTypes = GrantTypes.Code,
                    RequireConsent = false,
                    Claims = new List<Claim>{new Claim(JwtClaimTypes.Role, "WMTServerAPI.reader")},
                    AllowedScopes = { "openid", "profile", "WMTServerAPI.full_access", "WMTServerAPI.read_only" },
                    AlwaysSendClientClaims = true,
                    AllowedCorsOrigins = { "https://localhost:44355",
                        "https://localhost:44362",
                        "https://localhost:44320"
                    },
                    RedirectUris = { "https://localhost:44355/oidc/callbacks/authentication-redirect", 
                        "https://localhost:44362/oidc/callbacks/authentication-redirect",
                        "https://localhost:44320/oidc/callbacks/authentication-redirect"
                    },
                    RequireClientSecret = false
                }
            };
        }
    }
}