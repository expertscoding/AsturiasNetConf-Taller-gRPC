using System.Collections.Generic;
using IdentityModel;
using IdentityServer4.Models;

namespace OidC.Services.InMemory
{
    public class Resources
    {
        public static IEnumerable<IdentityResource> GetIdentityResources()
        {
            return new List<IdentityResource>
            {
                new IdentityResources.OpenId(),
                new IdentityResources.Profile(),
                new IdentityResources.Email()
            };
        }

        public static IEnumerable<ApiResource> GetApiResources()
        {
            return new[]
            {
                // simple version with ctor
                new ApiResource("WMTServerAPI", "Windmill Telemetry API")
                {
                    // this is needed for introspection when using reference tokens
                    ApiSecrets = { new Secret("API_AsturiasNetConf2020".Sha256()) },
                    Scopes =
                    {
                        new Scope
                        {
                            Name = "WMTServerAPI.full_access",
                            DisplayName = "Full access to API 2"
                        },
                        new Scope
                        {
                            Name = "WMTServerAPI.read_only",
                            DisplayName = "Read only access to API 2",
                        }
                    }
                }
            };
        }
        
    }
}