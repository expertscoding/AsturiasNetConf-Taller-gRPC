[< Volver al inicio](../README.md#exercises)

# Estructura del proyecto y librerías

Lo primero que necesitamos para el resto de ejercicios es una solución organizada y con las referencias a las librerías necesarias para compilar y ejecutar un servicio gRPC.

Por lo tanto vamos a abrir la solución de la carpeta `src/00-proyecto-base`. Aunque la estructura de proyectos ya está creada, es solo para ahorrar tiempo durante el taller, ya que los proyectos están completamente vacíos tal como son creados desde las plantillas de VS o dotnet.

>Partir del proyecto base nos servirá para explicar brevebemente para que sirve cada librería relativa a gRPC, pero la carpeta `src/01-estructura-proyecto-librerias` es la misma estructura de solución con las librerías ya referenciadas en cada proyecto. Si quieres saltarte ir instalando cada una, abre la solución de este directorio entonces.

## Los Proyectos

- `WMTServer`: proyecto de tipo ASP.Net Core, con características de Web API. Actuará de servidor en la solución.
- `WMTDashboard`: proyecto de tipo ASP.Net Core, con características MVC. Nos servirá para mostrar los datos que le pediremos al servidor, los cuales recibiremos a través de gRPC.
- `WMTLogger`: proyecto de consola .Net Core. Lo usaremos para mostrar la característica de streaming en servicios gRPC.
- `OidC`: proyecto de tipo ASP.Net Core, con características de Web API y MVC. Llegado el momento será nuestro servidor de autenticación, implementa OpenId Connect y OAuth2.
- `WMTBlazor\WMTBlazor.Shared`: librería de clases Net Standard compartida entre cliente/servidor.
- `WMTBlazor\WMTBlazor.Client`: Proyecto UI que compilará a WebAssembly y se ejecutará completamente en cliente.
- `WMTBlazor\WMTBlazor.Server`: Servidor desde el que los navegadores descargarán el código WASM, también nose servirá para albergar servicios adicionales requeridos por el cliente.

## Las librerías

Una vez que ya sabemos mínimamente que son todos estos proyectos de la solución podemos empezar a añadir las librerías que harán falta tanto para compilar como ejecutar la solución.

- [Google.Protobuf (3.11.3)](https://www.nuget.org/packages/Google.Protobuf/3.11.3): Serializador y deserializador para mensajes protobuf.
  - Instalar en: `WMTLogger`, `WMTDashboard` y `WMTBlazor\WMTBlazor.Shared`
- [Grpc.Tools 2.27.0](https://www.nuget.org/packages/Grpc.Tools/2.27.0): Herramientas para la autogeneración de código en diferentes lenguajes. Incluye además los imports por defecto escritos por Google.
  - Instalar en: `WMTLogger`, `WMTDashboard` y `WMTBlazor\WMTBlazor.Shared`
- [Grpc.Net.Client 2.27.0](https://www.nuget.org/packages/Grpc.Net.Client/2.27.0): Código base para actuar como cliente en el protocolo gRPC.
  - Instalar en: `WMTLogger` y `WMTBlazor\WMTBlazor.Shared`
- [Grpc.Net.ClientFactory 2.27.0](https://www.nuget.org/packages/Grpc.Net.ClientFactory/2.27.0): Código base para actuar como cliente en el protocolo gRPC además de los métodos de extensión necesarios para registrar en el contenedor de dependencias de AspNetCore el cliente Http.
  - Instalar solo en `WMTDashboard`
- [Grpc.Net.Client.Web 2.27.0-pre1](https://www.nuget.org/packages/Grpc.Net.Client.Web/2.27.0-pre1): Al igual que las anteriores tiene el código necesario para actuar como cliente pero en este caso para su uso desde el explorador usando gRPC-Web. (Recuerda activar el flag _Incluir versiones previas_)
  - Instalar en: `WMTServer` y `WMTBlazor\WMTBlazor.Client`
- [Grpc.AspNetCore.Web 2.27.0-pre1](https://www.nuget.org/packages/Grpc.AspNetCore.Web/2.27.0-pre1): Nos permitirá exponer los servicios gRPC para su uso desde el navegador en el ejemplo de Blazor.
  - Instalar solo en `WMTServer`

Por último, para el ejercicio en el que habilitaremos la autenticación, necesitaremos instalar en los proyectos `WMTDashboard` y `WMTLogger` las siguientes librerías: [IdentityModel](https://www.nuget.org/packages/IdentityModel/4.1.1), [Microsoft.Extensions.Caching.Memory](https://www.nuget.org/packages/Microsoft.Extensions.Caching.Memory/3.1.1) y [System.IdentityModel.Tokens.Jwt](https://www.nuget.org/packages/System.IdentityModel.Tokens.Jwt/5.6.0). En el proyecto `WMTServer` necesitaremos [IdentityServer4.AccessTokenValidation](https://github.com/IdentityServer/IdentityServer4.AccessTokenValidation).


Con los proyectos organizados y todas las librerías instaladas podemos empezar a crear servicios. Vámonos!

[Siguiente ejercicio >](02-creando-servicio-grpc.md)
