using Grpc.Net.Client;
using Grpc.Net.Client.Web;
using Microsoft.Extensions.DependencyInjection;
using Sotsera.Blazor.Oidc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using WMTServer;

namespace WMTBlazor.Client
{
    public static class ServiceExtensions
    {

        public static IServiceCollection AddGrpc(this IServiceCollection services)
        {

            services.AddSingleton(services =>
            {
                // Create a gRPC-Web channel pointing to the backend server
                var httpClient = new HttpClient(new GrpcWebHandler(GrpcWebMode.GrpcWeb, new HttpClientHandler()));
                var baseUri = new Uri("https://localhost:5001/");
                var channel = GrpcChannel.ForAddress(baseUri, new GrpcChannelOptions { HttpClient = httpClient });

                // Now we can instantiate gRPC clients for this channel
                return new WindmillFarm.WindmillFarmClient(channel);
            });

            return services;
        }

        public static IServiceCollection AddOidc(this IServiceCollection services)
        {

            services.AddOptions();
            services.AddAuthorizationCore();

            services.AddOidc(new Uri("https://localhost:5006"), (settings, siteUri) =>
            {
                settings.UseDefaultCallbackUris(siteUri);
                
                settings.ClientId = "WMTBlazor";
                settings.ResponseType = "code";
                settings.Scope = "openid profile WMTServerAPI.full_access WMTServerAPI.read_only";
            });

            return services;
        }
    }
}
