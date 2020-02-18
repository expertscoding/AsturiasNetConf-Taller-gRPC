[< Volver al inicio](../README.md#exercises)

[< Ejercicio anterior](05-autenticando-el-servicio.md)

# Versión de Blazor

Blazor WebAssembly aún está en preview y evoluciona realmente rápido, así que tenemos que cargar las últimas librerias (a fecha de escribir esto) en nuestro proyecto.

Para ello en la ventana de VS de **Package Manager Code** debemos poner el siguiente comando:

```dotnet new -i Microsoft.AspNetCore.Blazor.Templates::3.2.0-preview1.20073.1```

Ahora ya podemos crear nuestro proyecto Blazor.

Que ya lo habiamos creado?, no importa, quitadlo y volved a crearlo.

# Lo primero es la seguridad

No dejemos la seguridad para el final, que despues siempre se complica el tema...

Aunque podriamos usar el componente js oidc-client directamente, va a ser más sencillo implementar un flujo de autenticacion de usuarios en OidC con un cliente para Blazor. Para ello usaremo la siguiente libreria:

```Install-Package Sotsera.Blazor.Oidc -Version 1.0.0-alpha-6```

Ahora vamos a modificar nuestra apliacion para securizarla mínimamente.

- Asegurarnos que nuestra App sabe enrutar las peticiones con seguridad, para ello hay que cambiar el fichero `App.razor` que deberia quedar:

```csharp
@using Sotsera.Blazor.Oidc
@inject IUserManager UserManager

<Router AppAssembly="@typeof(Program).Assembly" AdditionalAssemblies="new[] { typeof(IUserManager).Assembly }">
    <Found Context="routeData">
        <AuthorizeRouteView RouteData="@routeData" DefaultLayout="@typeof(MainLayout)">
            <NotAuthorized>
                <p>You're not authorized to reach this page.</p>
            </NotAuthorized>
            <Authorizing>
                <h3>Authentication in progress</h3>
            </Authorizing>
        </AuthorizeRouteView>
    </Found>
    <NotFound>
        <CascadingAuthenticationState>
            <LayoutView Layout="@typeof(MainLayout)">
                <p>Sorry, there's nothing at this address.</p>
            </LayoutView>
        </CascadingAuthenticationState>
    </NotFound>
</Router>
```

Esto nos añadirá los componentes de seguridad en el enrutado de las páginas de Blazor aunque seguramente no reconozca algunos espacios de nombres, para ello debemos declarar los usings correspondientes, lo más facil es hacerlo para todos los componentes razor en el fichero **_Imports.razor**

```csharp
@using Microsoft.AspNetCore.Components.Authorization
@using WMTBlazor.Client.Components
```

(El segundo using nos vendrá bien más adelante, pero que ya pasabamos por aquí...)

Podemos saber de qué va todo esto en el siguiente enlace [https://docs.microsoft.com/en-us/aspnet/core/security/blazor/?view=aspnetcore-3.1&tabs=visual-studio](https://docs.microsoft.com/en-us/aspnet/core/security/blazor/?view=aspnetcore-3.1&tabs=visual-studio)

- Añadir un componente razor de Login en una nueva carpeta **Components** en el proyecto cliente con el siguiente código:

```csharp
@inject Sotsera.Blazor.Oidc.IUserManager UserManager
<AuthorizeView>
    <Authorized>
        <span class="login-display-name mr-3">
            Hello, @context.User.Identity.Name!
        </span>
        <button type="button" class="btn btn-primary btn-sm" @onclick="LogoutRedirect">
            Log out
        </button>
    </Authorized>
    <NotAuthorized>
        <button type="button" class="btn btn-primary btn-sm" @onclick="LoginRedirect">
            Log in
        </button>
    </NotAuthorized>
</AuthorizeView>

@code
{
    public async void LoginRedirect() => await UserManager.BeginAuthenticationAsync(p => p.WithRedirect());

    public async void LogoutRedirect() => await UserManager.BeginLogoutAsync(p => p.WithRedirect());
}
```

- Modificar el fichero `Shared/MainLayout.razor` para que tenga en cuenta si el usuario está autenticado:

```csharp
<AuthorizeView>
    <Authorized>
        <div class="sidebar">
            <NavMenu />
        </div>

        <div class="main">
            <div class="top-row px-4">
                <Login />
                <a href="http://blazor.net" target="_blank" class="ml-md-auto">About</a>
            </div>

            <div class="content px-4">
                @Body
            </div>
        </div>
    </Authorized>
    <NotAuthorized>
        <div class="main">
            <div class="top-row px-4">
                <Login />
            </div>

            <div class="content px-4">
                You better log in!
            </div>
        </div>
    </NotAuthorized>
</AuthorizeView>
```

Como el componente de Login tambien sabe diferenciar si el usuario está autenticado, en cada caso mostrará los controles de Log in o Log out correspondientes.

- Añadir el poco js que ayuda en el procesado del flujo de autenticación. Esto lo haremos en el fichero `wwwroot/index.html`

`<script src="_content/Sotsera.Blazor.Oidc/sotsera.blazor.oidc-1.0.0-alpha-6.js"></script>`

Casi lo tenemos, solo queda configurar la seguridad al inicio de la aplicación!

- No busqueis el fichero **Startup**. Ya no existe en la parte de cliente de Blazor! Ahora se configura todo en **Program.cs**. Para ayudarnos un poco a que todo esté más organizado, vamos a introducir los servicios a traves de métodos de extensiones, en una nueva clase `ServiceExtensions.cs`.

```csharp
public static IServiceCollection AddOidc(this IServiceCollection services)
{

    services.AddOptions();
    services.AddAuthorizationCore();

    services.AddOidc(new Uri("https://localhost:5006"), (settings, siteUri) =>
    {
        settings.UseDefaultCallbackUris(siteUri);

        settings.ClientId = "WMTBlazor";
        settings.ClientSecret = "Blzr_AsturiasNetConf2020";
        settings.ResponseType = "code";
        settings.Scope = "openid profile WMTServerAPI.full_access WMTServerAPI.read_only";
    });

    return services;
}
```

Por no meternos en más jaleos, dejaremos la ip y la configuracion de acceso directamente en el código.

Ahora, en `Program.cs`, debemos invocar este método de extension:

```csharp
var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddOidc();

builder.RootComponents.Add<App>("app");
```

Y, por fin, podemos darle a F5, aseguremonos que tenemos corriendo el proyecto de Oidc. En el login, deberemos usar antonio.marin@expertscoding.es/Pass1234$ (superseguro)

# Añadiendo gRPC
Lo primero que debemos saber es que no podemos llamar a gRPC directamente desde aplicaciones en explorador. Por qué? porque los exploradores no exponen ningun API para realizar llamadas htttp 2 y solo podemos realizar llamadas http 1.1.

Para solucionar esto, hay una libreria js que hace las llamadas a gRPC a traves de http1.1, pero nuestros servicios deben estar preparados para ello.

Preparando nuestros servicios:

- Añadir la libreria que nos deja acceder a los métodos gRPC via http 1.1:  
`Install-Package Grpc.AspNetCore.Web -Version 2.27.0-pre1`
- Añadir el wrapper a los endpoints: en cada endpoint, debemos añadir el método de extensión EnableGrpcWeb(), quedando:

```csharp
endpoints.MapGrpcService<WindmillFarmService>().EnableGrpcWeb();
endpoints.MapGrpcService<WindmillTelemeterService>().EnableGrpcWeb();
```

Realizando las llamadas:

- Añadir el el cliente de los servicios gRPC: este proceso es muy parecido al paso 03, pero lo haremos en el proyecto Shared de Blazor.
- Añadir las Llamadas en el Cliente:

Lo primero es añadir la libreria correspondiente para que realice las llamadas gRPC-web:

```csharp
Install-Package Grpc.Net.Client.Web -Version 2.27.0-pre1
```

Lo siguiente es añadir un método de extension para configurarlo en ServiceExtensions:

```csharp
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
```

Ahora deberemos realizar la invocación desde alguna página razor de la aplicación. Lo haremos desde `FetchData.razor`, quedando su código:

```csharp
@page "/fetchdata"
@using WMTServer
@inject WindmillFarm.WindmillFarmClient client
@inject Sotsera.Blazor.Oidc.IUserManager UserManager

@if (windmillData == null)
{
    <p><em>Loading...</em></p>
}
else
{
    <div class="text-center">
        <h1 class="display-4">Windmill Farm Dashboard</h1>
        <p>Windmills in the farm:</p>
    </div>
    <div>
        <table class="table  table-striped">
            <thead>
                <tr>
                    <th scope="col"></th>
                    <th class="text-center" scope="col" colspan="2">Average Power</th>
                </tr>
                <tr>
                    <th scope="col">Windmill Id</th>
                    <th class="text-center" scope="col">Last minute</th>
                    <th class="text-center" scope="col">Last hour</th>
                </tr>
            </thead>
            <tbody>
                @foreach (var windmill in windmillData)
                {
                    <tr>
                        <th scope="row">@windmill.WindmillId</th>
                        <td class="text-center">@windmill.AvgPowerGeneratedLastMinute.ToString("F") W/h</td>
                        <td class="text-center">@windmill.AvgPowerGeneratedLastHour.ToString("F") W/h</td>
                    </tr>
                }
            </tbody>
        </table>
    </div>
}

@code {
    private List<WindmillInfo> windmillData;

    protected override async Task OnInitializedAsync()
    {
        var authHeader = new Grpc.Core.Metadata { {"Authorization", "Bearer " + UserManager.UserState.AccessToken} };

        var response = await client.RequestListAsync(new WindmillListRequest(), authHeader);

        windmillData = response.Windmills.ToList();
    }
}
```

Sorprendentemente, todo esto deberia funcionar, siempre que tengamos levantados los proyectos de `OidC` y de `WMTServer`.

NO? qué raro? nos faltará algo? alguien lo sabe?

[< Volver al inicio](../README.md#exercises)
