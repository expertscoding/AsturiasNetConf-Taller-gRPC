[< Volver al inicio](../README.md#exercises)

[< Ejercicio anterior](01-estructura-proyecto-librerias.md)

# Creando un servicio gRPC

Como decíamos, ahora sí podemos mancharnos las manos creando nuestros servicios gRPC.  
El trabajo de este ejercicio se va a centrar en el proyecto `WMTServer`.  
Si te fijas, en la carpeta `Protos` hay un servicio que se crea por defecto con la plantilla, directamente puedes borrarlo para crear los nuestros.

>Ten a mano la referencia del lenguaje protobuf por si necesitamos consultarla:  
[https://developers.google.com/protocol-buffers/docs/proto3](https://developers.google.com/protocol-buffers/docs/proto3)  

En dicha carpeta carpeta crea el fichero `Enums.proto` con el siguiente contenido:

```protobuf
syntax = "proto3";

option csharp_namespace = "WMTServer";

package Windmill;

enum WindDirection {
  N = 0;
  NE = 1;
  E = 2;
  SE = 3;
  S = 4;
  SW = 5;
  W = 6;
  NW = 7;
}

enum WindmillStatus {
  Disconnected = 0;
  Starting = 1;
  Connected = 2;
  Stopping = 3;
}
```
Este solo tiene un par de enumeraciones que utilizaremos a continuación. Como puedes ver el fichero empieza con `syntax = "proto3";` esto es norma y sirve para indicarle al generador de código que versión de protobuf queremos usar. A continuación instrucciones para la  herramienta sobre donde queremos situar el código autogenerado y que todo esto forma parte del paquete `Windmill`.

El siguiente archivo será `WindmillFarm.proto` donde pegaremos estas líneas:

```protobuf
syntax = "proto3";
import "Enums.proto";

option csharp_namespace = "WMTServer";

package Windmill;

service WindmillFarm {
  rpc RequestList (WindmillListRequest) returns (WindmillListResponse);
  rpc RequestWindmillStatus (WindmillStatusRequest) returns (WindmillInfo);
  rpc DisconnectFromGrid (.Windmill.WindmillStatusRequest) returns (WindmillInfo);
}

message WindmillListRequest {}

message WindmillListResponse {
  int32 TotalCount = 1;

  double AvgPowerGeneratedLastMinute = 2;

  double AvgPowerGeneratedLastHour = 3;

  repeated WindmillInfo Windmills = 4;
}

message WindmillStatusRequest {
  string WindmillId = 1;
}

message WindmillInfo {
  string WindmillId = 1;

  double AvgPowerGeneratedLastMinute = 2;

  double AvgPowerGeneratedLastHour = 3;

  Windmill.WindmillStatus Status = 4;
}
```

Al inicio la novedad de importar nuestro archivo `Enums.proto` anterior.  
A continuación la definición propia del servicio y sus métodos. Ten en cuenta que cada método del servicio **siempre** tiene que tener un tipo de entrada y otro de salida, con esto se favorece la compatibilidad entre las diferentes versiones de los mismos. Si tienes claro que tu servicio nunca devolverá o recibirá nada puedes usar los tipos predefinidos por gRPC como `Any` los puedes en el contenido del paquete nuget o descargando el código de gRPC.  
Por último, los mensajes propios que utilizamos como entrada y salida en los servicios. Fíjate que sus propiedades están numeradas, igualmente esto sirve para mantener la compatibilidad entre versiones, ya que los tipos siempre tendrán un valor por defecto, así que para modificar los mensajes sin romper los clientes es tan sencillo como añadir las propiedades siguiendo el autoincremental.

Añadamos un servicio más, crea otro archivo con el nombre `WindmillTelemeter.proto` y de contenido:

```protobuf
syntax = "proto3";
import "Enums.proto";

option csharp_namespace = "WMTServer";

package Windmill;

// The service definition.
service WindmillTelemeter {
  // Sends Telemetry.
  rpc RequestTelemetry (WindmillInfoRequest) returns (WindmillTelemetryResponse);
  rpc RequestTelemetryStream (WindmillInfoRequest) returns (stream WindmillTelemetryResponse);
}

message WindmillInfoRequest {
  string WindmillId = 1;
}

// The response message containing actual values about the windmill.
message WindmillTelemetryResponse {
  string WindmillId = 1;

  string EventTime = 2;

  double WindSpeed = 3;

  int32 RPM = 4;

  Windmill.WindDirection WindDirection = 5;

  double PowerOutput = 6;

  double VoltageOutput = 7;
}
```

Para los tres archivos creados en la carpeta Protos debemos modificar sus propiedades para establecer _Build Action_ a _Protobuf compiler_ y _gRPC Stub Classes_ en _Server only_.

>Para que la importación entre archivos funcione correctamente es necesario editar el archivo de proyecto manualmente para añadir la propiedad AdditionalImportDirs a los dos servicios proto, quedarían tal que así:  
`<Protobuf Include="Protos\WindmillFarm.proto" GrpcServices="Server" AdditionalImportDirs="Protos\" />`  
`<Protobuf Include="Protos\WindmillTelemeter.proto" GrpcServices="Server" AdditionalImportDirs="Protos\" />`  
de lo contrario al compilar obtendremos un bonito error diciendo: _Import "Enums.proto" was not found or had errors._  

Para darle un poco de funcionalidad al servicio hay algo de _fontanería_ para generar datos. Si sigues incrementando el proyecto base, copia entonces los ficheros de `src/02-creando-servicio-grpc/WMTServer/Mock` a la misma carpeta en nuestro servidor.

Además tenenemos que crear los servicios que vamos a exponer, para lo que generaremos un par de clases que heredarán del código autogenerado desde la definición de los protos. Borra de la carpeta Services el archivo GreeterService.cs.  
Crea un nuevo archivo `WindmillFarmService.cs` donde pondremos este código:

```csharp
using System;
using System.Linq;
using System.Threading.Tasks;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using WMTServer.Mock;

namespace WMTServer
{
    public class WindmillFarmService : WindmillFarm.WindmillFarmBase
    {
        private readonly ILogger<WindmillFarmService> logger;
        private readonly WindmillsDataReader windmillsDataReader;

        public WindmillFarmService(ILogger<WindmillFarmService> logger, WindmillsDataReader windmillsDataReader)
        {
            this.logger = logger;
            this.windmillsDataReader = windmillsDataReader;
        }

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

A destacar de esta clase es la herencia desde `WindmillFarm.WindmillFarmBase`, efectivamente esta clase se autogenera desde el archivo proto y tiene lo necesario para que nuestro servidor sepa que es un servicio gRPC.  
Además podemos (y debemos) sobreescribir cada uno de los métodos del servicio para dotarlo de funcionalidad, fíjate que va a recibir el objeto request tal como lo definimos en el proto correspondiente y un contexto con el que podemos interactuar con las propiedades del protocolo; también es necesario devolver un objeto del tipo que definimos.  
Ejemplo: `public override Task<WindmillListResponse> RequestList(WindmillListRequest request, ServerCallContext context)`

Crear otro archivo `WindmillTelemeterService.cs` para el servicio restante con el código:

```csharp
using System;
using System.Linq;
using System.Threading.Tasks;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using WMTServer.Mock;

namespace WMTServer
{
    public class WindmillTelemeterService : WindmillTelemeter.WindmillTelemeterBase
    {
        private readonly ILogger<WindmillTelemeterService> _logger;
        private readonly WindmillsDataReader windmillsDataReader;

        public WindmillTelemeterService(ILogger<WindmillTelemeterService> logger, WindmillsDataReader windmillsDataReader)
        {
            _logger = logger;
            this.windmillsDataReader = windmillsDataReader;
        }

        public override Task<WindmillTelemetryResponse> RequestTelemetry(WindmillInfoRequest request, ServerCallContext context)
        {
            return Task.FromResult(windmillsDataReader.GetTelemetryValues(Guid.Parse(request.WindmillId)).LastOrDefault());

        }

        public override async Task RequestTelemetryStream(WindmillInfoRequest request, IServerStreamWriter<WindmillTelemetryResponse> responseStream, ServerCallContext context)
        {
            while (!context.CancellationToken.IsCancellationRequested)
            {
                //TODO add business logic to receive last event only once
                var last = windmillsDataReader.GetTelemetryValues(Guid.Parse(request.WindmillId)).LastOrDefault();
                _logger.LogInformation($"Sending windmill info for {last?.WindmillId} at {last?.EventTime}.");

                await responseStream.WriteAsync(last);

                // simulemos más trabajo
                await Task.Delay(1000);
            }
        }
    }
}
```

Para terminar, configuremos ahora la clase `Startup` con el siguiente código:

```csharp
public void ConfigureServices(IServiceCollection services)
{
    //Registro de la fontanería
    services.AddSingleton<WindmillDataStore>();
    services.AddTransient<WindmillsDataReader>();
    services.AddHostedService<DataGeneratorService>();

    services.AddGrpc();
}

public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
{
    if (env.IsDevelopment())
    {
        app.UseDeveloperExceptionPage();
    }

    app.UseRouting();

    app.UseEndpoints(endpoints =>
    {
        //Registro de nuestros servicios gRPC en el enrutamiento de AspNetCore
        endpoints.MapGrpcService<WindmillFarmService>();
        endpoints.MapGrpcService<WindmillTelemeterService>();

        endpoints.MapGet("/", async context =>
        {
            await context.Response.WriteAsync("Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");
        });
    });
}

```

Lo más importante de la configuración de la aplicación es la llamada a `services.AddGrpc()` y el registro adecuado de cada servicio con `endpoints.MapGrpcService<TService>()`. No lo olvides o tu servidor no funcionará.

Y con todo esto ya tenemos el servidor montado, podemos ejecutarlo y... No veremos nada, pero te aseguro que el servidor está ahí 😉

Sigamos para crear un cliente y demostrar que esto funciona.

[Siguiente ejercicio >](03-creando-cliente-dotnet-grpc.md)
