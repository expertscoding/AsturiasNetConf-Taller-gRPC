using IdentityServer4.AccessTokenValidation;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using WMTServer.Mock;

namespace WMTServer
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<WindmillDataStore>();
            services.AddTransient<WindmillsDataReader>();
            services.AddHostedService<DataGeneratorService>();
            
            var authOptions = Configuration.GetSection(nameof(IdentityServerAuthenticationOptions));

            services.AddAuthentication()
                .AddIdentityServerAuthentication(options => authOptions.Bind(options));

            services.AddAuthorization(options =>
            {
                options.AddPolicy("ApiAdmin", policy => policy.Requirements.Add(new ClaimsAuthorizationRequirement("client_role", new[] { "Administrator" })));
                options.AddPolicy("ApiReader", policy => policy.Requirements.Add(new ClaimsAuthorizationRequirement("client_role", new[] { "WMTServerAPI.reader" })));
            });
            

            services.AddGrpc();

            services.AddCors();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseCors(config =>
            {
                config.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin();
            });

            app.UseRouting();

            app.UseGrpcWeb();

            
            app.UseAuthentication();
            app.UseAuthorization();
            

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapGrpcService<WindmillFarmService>().EnableGrpcWeb();
                endpoints.MapGrpcService<WindmillTelemeterService>().EnableGrpcWeb();

                endpoints.MapGet("/", async context =>
                {
                    await context.Response.WriteAsync("Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");
                });
            });
        }
    }
}
