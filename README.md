# Asturias NetConf - Taller gRPC

En este repositorio puedes encontrar el código de del taller ¿Cómo, cuándo y por qué usar gRPC? impartido en la Asturias NetConf.

## Requisitos

Esto es lo que te hará falta que tráigas previamente instalado para el taller:

- **Visual Studio 2019**: A ser posible la versión **Preview**, ya que uno de los ejercicios será una aplicación de Blazor WebAssembly. De lo contrario este ejercicio solo podrás verlo correr en el proyector del taller. Es válida cualquier versión: Community, Professional o Enterprise. [https://visualstudio.microsoft.com/](https://visualstudio.microsoft.com/)
- **SDK .Net Core 3.1**: Lo puedes descargar desde [https://dotnet.microsoft.com/download/dotnet-core/3.1](https://dotnet.microsoft.com/download/dotnet-core/3.1)
- [Opcional] **Compilador GO Lang**: Descargalo desde [https://golang.org/dl/](https://golang.org/dl/). Durante el taller haremos un pequeño ejemplo de como reutilizar las definiciones en múltiples lenguajes, concretamente en GO. Si queires realizar este ejercicio te hará falta el compilador (v1.6+). Además te hará falta el compilador _protoc_ que puedes encontrar en [https://github.com/google/protobuf/releases](https://github.com/google/protobuf/releases). Por último instalar gRPC para GO mediante el comando `go get -u google.golang.org/grpc`
- [Opcional] **VS Code**: Este editor de código puede ser útil para revisar el ejemplo de GO si vas a realizarlo. Por otro lado puede servir de alternativa a VS 2019, pero ten en cuenta que puede que haya algunos ejemplos que no funcionen en VS Code.

### Imporante

>Para que durante el taller no haya problemas de acceso a los paquetes nuget te recomendamos que compiles la solución del directorio final antes de venir. Esto hará que se restauren los paquetes necesarios y se almacenen en la cache que mantiene nuget en tu equipo.  
Puedes abrir la solución directamente en VS2019 y compilar, o bien desde la línea de comandos donde hayas clonado el repositorio ejecutar:  
>`dotnet build final\WindmillTelemetry.sln`  
>&nbsp;

## Estructura del repositorio

Antes de nada, decir que vamos a crear un servicio simulado que devuelva información de telemetría de los generadores en un parque eólico.  
Podrás ver varias carpetas en el raíz del repositorio:

- `src`: En esta carpeta encontrarás la structura de proyectos inicial sin código, sobre el que iremos haciendo ejercicios para completarlo. Además verás diferentes carpetas con cambios incrementales correspondientes a los ejercicios del taller.
- `cmd`: Código de un programa sencillo en GO donde realizaremos una práctica para reutilizar definiciones de servicios y probar la conexión con el código en .Net Core.
- `final`: En este directorio encontrarás el código terminado y funcionando tal como nos quedará a la finalización del taller, para que puedas revisarlo tranquilamente en casa por si algo no fué bien durante los ejercicios.

## <a name="exercises"></a>¡Empecemos!

Ahora sí, llega el momento de empezar a mancharnos las manos de barro.  
Si todavía no has clonado este repositorio, ya estás tardando. Usa el botón verde en Github para clonarlo, descárgalo como zip o ejecuta el comando:

```bash
git clone https://github.com/expertscoding/AsturiasNetConf-Taller-gRPC.git
```

Te recomendamos que para empezar a crear servicios, le eches un ojo primero a la sintaxis de protobuf que es sobre lo que se construye gRPC:  
[https://developers.google.com/protocol-buffers/docs/proto3](https://developers.google.com/protocol-buffers/docs/proto3)  
No es imprescindible saberse la sintaxis pero sí al menos las cosas básicas que podemos crear (Servicios, Enumeraciones, Tipos, etc), del resto hablaremos durante el taller.

Seguiremos este listado de ejercicios, de los cuales el código lo puedes encontrar en la carpeta `src/<XX-nombre-ejercicio>`:

1. [Estructura del proyecto y librerías](.instructions/01-estructura-proyecto-librerias.md)
2. [Creando un servicio gRPC](.instructions/02-creando-servicio-grpc.md)
3. [Creando un cliente en .Net Core para el servicio](.instructions/03-creando-cliente-dotnet-grpc.md)
4. [Probemos a crear un cliente en GO](.instructions/04-creando-cliente-go-grpc.md)
5. [Introduciendo la autenticación en el servicio](.instructions/05-autenticando-el-servicio.md)
6. [Llevando gRPC al navegador con Blazor y WebAssembly](.instructions/06-gRPC-navegador-blazor-webassembly.md)

Si has llegado hasta aquí termiando todos los ejercicios: Enhorabuena!! si no también!
Recuerda que en la carpeta `final` tienes todo el código terminado para revisarlo tranquilamente y echarlo a correr.
