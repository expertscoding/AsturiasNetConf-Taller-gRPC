using System.IdentityModel.Tokens.Jwt;
using System.Threading.Tasks;
using IdentityModel.Client;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace WMTLogger
{
    public class AccessTokenManager
    {
        public const string TokenResponseKey = "API_TOKEN_RESPONSE";

        private readonly ILogger<AccessTokenManager> logger;
        private readonly IMemoryCache cache;
        private readonly TokenClient client;
        private readonly ApiClientOptions clientOptions;

        public AccessTokenManager(ILogger<AccessTokenManager> logger, IMemoryCache cache, TokenClient client, IOptions<ApiClientOptions> clientOptions)
        {
            this.logger = logger;
            this.cache = cache;
            this.client = client;
            this.clientOptions = clientOptions.Value;
        }

        public async Task<TokenResponse> GetApiTokenAsync()
        {
            var apiToken = cache.Get<TokenResponse>(TokenResponseKey);
            if (apiToken == null)
            {
                logger.LogDebug($"Requesting new token for {clientOptions.TokenClientOptions.ClientId}");
                var response = await client.RequestClientCredentialsTokenAsync(clientOptions.Scopes);

                var jwtToken = new JwtSecurityToken(response.AccessToken);
                var tokenExpiration = jwtToken.ValidTo.ToUniversalTime().AddMinutes(-5);

                cache.Set(TokenResponseKey, response, tokenExpiration);
                apiToken = response;
            }

            return apiToken;
        }
    }
}