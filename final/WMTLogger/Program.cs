using System;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using Grpc.Net.Client;
using IdentityModel.Client;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using WMTServer;

namespace WMTLogger
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var apiOptions = new ApiClientOptions
            {
                Scopes = "WMTServerAPI.full_access WMTServerAPI.read_only",
                TokenClientOptions = new TokenClientOptions { Address = "https://localhost:5006/connect/token", ClientId = "WMTServerDashboard", ClientSecret = "Client_AsturiasNetConf2020" }
            };

            using var httpClient = new HttpClient();
            var tokenClient = new TokenClient(httpClient, apiOptions.TokenClientOptions);
            var tokenManager = new AccessTokenManager(new NullLogger<AccessTokenManager>(), new MemoryCache(new MemoryCacheOptions()), tokenClient, new OptionsWrapper<ApiClientOptions>(apiOptions));

            using var channel = GrpcChannel.ForAddress("https://localhost:5001");
            var client = new WindmillFarm.WindmillFarmClient(channel);

            var authHeader = new Metadata { {"Authorization", "Bearer " + (await tokenManager.GetApiTokenAsync()).AccessToken} };

            var windmills = client.RequestList(new WindmillListRequest(), authHeader);
            var guid = windmills.Windmills.FirstOrDefault()?.WindmillId;

            if (Guid.TryParse(guid, out var windmillId))
            {
                Console.WriteLine($"Windmill info for {guid}");
                await TelemetryStreaming(windmillId, channel);
            }

            Console.WriteLine("Shutting down client");
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }

        private static async Task TelemetryStreaming(Guid guid, GrpcChannel channel)
        {
            var cts = new CancellationTokenSource();
            cts.CancelAfter(TimeSpan.FromSeconds(27));

            var client = new WindmillTelemeter.WindmillTelemeterClient(channel);

            using var call = client.RequestTelemetryStream(new WindmillInfoRequest{WindmillId = guid.ToString()}, cancellationToken: cts.Token);
            try
            {
                await foreach (var wi in call.ResponseStream.ReadAllAsync(cancellationToken: cts.Token))
                {
                    Console.WriteLine($"Windmill info at {wi.EventTime}: {{RPM={wi.RPM},Power={wi.PowerOutput},Voltage={wi.VoltageOutput}}}");
                }
            }
            catch (RpcException ex) when (ex.StatusCode == StatusCode.Cancelled)
            {
                Console.WriteLine("Cancelling telemetry reading.");
            }
        }
    }
}
