[< Volver al inicio](../README.md#exercises)

[< Ejercicio anterior](04-creando-cliente-go-grpc.md)

# Introduciendo la autenticación en el servicio

No nos gustan los curiosos, así que necesitamos proteger el servicio para que solo los clientes/usuarios autenticados puean leer la información de telemetría.

El protocolo gRPC deja la autenticación como punto de extensión para que cada framework de desarrollo la integre de la mejor forma posible, y eso justo es lo que han hecho en .Net Core.  
Autenticar nuestro servicio es exáctamente igual que autenticar cualquier otro tipo de aplicación, así que vamos a ver una posible solución usando [OpenId Connect](https://openid.net/) y [OAuth2](https://www.oauth.com/).

Te habrás fijado que en la solución hay un proyecto llamado `OidC` con el que todavía no habíamos interacturado. Este será nuestro servidor de autenticación y sin entrar en mucho detalle te diré que usa [Identity Server 4](https://github.com/IdentityServer/IdentityServer4) además de tener la configuración de clientes y usuarios de forma estática para facilitar el desarrollo.

## Securizando el servidor

Lo primero que haremos será configurar el servidor para que requiera autenticación en las peticiones que lleguen.  
Abre la clase `Startup` para dejarla como este código:

```csharp
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
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();
            
            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapGrpcService<WindmillFarmService>();
                endpoints.MapGrpcService<WindmillTelemeterService>();

                endpoints.MapGet("/", async context =>
                {
                    await context.Response.WriteAsync("Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");
                });
            });
        }
    }
}
```

Como ves hemos añadido la llamada a `AddAuthentication()` configurandolo para validar la un Bearer token en identity server.  
Además configuramos un par de políticas de autorización mediante la llamada `AddAuthorization()`.
Después en el método `Configure()` añadimos las líneas `UseAuthentication()` y `UseAuthorization` para que AspNetCore tenga en cuanta los middleware configurados.

Solo faltaría establecer en los controladores que las llamadas deben ser autorizadas añadiendo el atributo `[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]`.  
Edita la clase `WindmillFarmService` para dejarla como sigue:

```csharp
using System;
using System.Linq;
using System.Threading.Tasks;
using Grpc.Core;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using WMTServer.Mock;

namespace WMTServer
{
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class WindmillFarmService : WindmillFarm.WindmillFarmBase
    {
        private readonly ILogger<WindmillFarmService> logger;
        private readonly WindmillsDataReader windmillsDataReader;

        public WindmillFarmService(ILogger<WindmillFarmService> logger, WindmillsDataReader windmillsDataReader)
        {
            this.logger = logger;
            this.windmillsDataReader = windmillsDataReader;
        }

        [Authorize("ApiReader")]
        public override Task<WindmillListResponse> RequestList(WindmillListRequest request, ServerCallContext context)
        {
            var windmillInfos = windmillsDataReader.GetWindmills();
            var response = new WindmillListResponse
            {
                TotalCount = windmillInfos.Count,
                AvgPowerGeneratedLastMinute = windmillInfos.Sum(wi => wi.AvgPowerGeneratedLastMinute),
                AvgPowerGeneratedLastHour = windmillInfos.Sum(wi => wi.AvgPowerGeneratedLastHour)
            };
            response.Windmills.Add(windmillInfos);
            return Task.FromResult(response);
        }

        public override Task<WindmillInfo> RequestWindmillStatus(WindmillStatusRequest request, ServerCallContext context)
        {
            return Task.FromResult(windmillsDataReader.GetWindmillInfo(Guid.Parse(request.WindmillId)));
        }

        [Authorize("ApiAdmin")]
        public override Task<WindmillInfo> DisconnectFromGrid(WindmillStatusRequest request, ServerCallContext context)
        {
            //TODO: Add business logic to disconnect the windmill
            var windmillInfo = windmillsDataReader.GetWindmillInfo(Guid.Parse(request.WindmillId));
            windmillInfo.Status = WindmillStatus.Disconnected;
            return Task.FromResult(windmillInfo);
        }
    }
}
```

Ten en cuenta que para usar las politicas de autorización configuradas debemos utilizarlas donde nos interesen, como en el caso del método `RequestList` o `DisconnectFromGrid`.

## Turno para el cliente

Una vez que tenemos el servidor listo, configuremos el cliente o sino habremos roto todo.

Empecemos con un poco de fontanería. Para facilitarnos la vida vamos a usar una clase auxiliar que pida un token cuando sea necesario y así podamos usarlo en la llamada al servicio.

En el raíz del proyecto `WMTDashboard` crea un archivo `AccessTokenManager.cs` con este contenido:

```csharp
using System.IdentityModel.Tokens.Jwt;
using System.Threading.Tasks;
using IdentityModel.Client;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace WMTDashboard
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
```

Este _manager_ no tiene otra función que la de pedir un token cuando no tenga o si el que hemos guardado en la cache ha caducado. Lo inyectaremos en el constructor de nuestra vista para obtener el token en el momento de hacer la llamada.

Ahora otro más con el nombre `ApiClientOptions.cs` para las opciones de configuración (las usaremos en la configuración del `Startup`):

```csharp
using IdentityModel.Client;

namespace WMTDashboard
{
    public class ApiClientOptions
    {
        public TokenClientOptions TokenClientOptions { get; set; }

        public string Scopes { get; set; }
    }
}
```

Registremos todo lo necesario en la clase `Startup` para que esté disponible en nuestro controlador:

```csharp
using System;
using IdentityModel.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using WMTServer;

namespace WMTDashboard
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddGrpcClient<WindmillFarm.WindmillFarmClient>(options => options.Address = new Uri(Configuration["WindmillServiceEndpoint"]));

            var apiClientOptions = Configuration.GetSection(nameof(ApiClientOptions));
            services.Configure<ApiClientOptions>(apiClientOptions);
            services.AddMemoryCache();
            services.AddSingleton(apiClientOptions.GetSection(nameof(TokenClientOptions)).Get<TokenClientOptions>());
            services.AddHttpClient<TokenClient>();
            services.AddTransient<AccessTokenManager>();

            services.AddControllersWithViews();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }
            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
```

Lo que hay aquí de nuevo es la lectura de la configuración `ApiClientOptions`, el registro de un proveedor de cache en memoria, el registro del tipo `TokenClient` con la característica `HttpClientFactory` disponible en .Net Core. Este `TokenClient` pertenece a una librería externa y su finalidad es la de comunicarse con el servidor de autenticación usando el protocolo OpenId Connect.  
Finalmente registramos nuestro `AccessTokenManager`.

Con todo debidamente configurado en `Startup` dejemos el controlador `HomeController` con este código:

```csharp
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Grpc.Core;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using WMTDashboard.Models;
using WMTServer;

namespace WMTDashboard.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> logger;
        private readonly WindmillFarm.WindmillFarmClient client;
        private readonly AccessTokenManager tokenManager;

        public HomeController(ILogger<HomeController> logger, WindmillFarm.WindmillFarmClient client, AccessTokenManager tokenManager)
        {
            this.logger = logger;
            this.client = client;
            this.tokenManager = tokenManager;
        }

        public async Task<IActionResult> Index()
        {
            var authHeader = new Metadata { {"Authorization", "Bearer " + (await tokenManager.GetApiTokenAsync()).AccessToken} };
            var response = await client.RequestListAsync(new WindmillListRequest(), authHeader);

            return View(response.Windmills.ToList());
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
```

Como ves, hemos añadido como dependencia en el constructor el `AccessTokenManager` para usarlo en la acción `Index`, donde creamos un objeto `Metadata` con la autenticación que nuestro servicio requiere ahora.

La configuración de el cliente `WMTLogger` es prácticamente igual solo que debemos copiar el `AccessTokenManager` y `ApiClientOptions` creados anteriormente y configurar todo en el método `Main`. Veamos como quedaría la clase:

```csharp
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
```

Lo único que hemos hecho es crear al inicio todas las clases que en la web el contenedor de dependencias hubiera creado por nosotros. Luego al igual que en el controller hemos obtenido el token y añadido a la request del servicio.

Y así de simple es autenticar un servicio gRPC, teniendo en cuenta todo el trabajo adicional que lleva el mecanismo de autenticación que elijamos. Lo que tenemos que tener claro en este punto es que gRPC en AspNetCore nos permite usar el flujo de autenticación que queramos ya que lo interpone en una capa por encima del endpoint del servicio.

Vamos llegando al final! El último ejercicio es la guinda que le falta al workshop, consumir el servicio desde el navegador del cliente.

[Siguiente ejercicio >](06-gRPC-navegador-blazor-webassembly.md)
