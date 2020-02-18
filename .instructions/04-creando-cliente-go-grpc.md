[< Volver al inicio](../README.md#exercises)

[< Ejercicio anterior](03-creando-cliente-dotnet-grpc.md)

# Creando un cliente en GO para el servicio

Un poco de Rock & Roll. Vamos a hacernos un cliente en GO!

Recuerda que necesitas requisitos adicionales para este ejercicio, en concreto el [compilador de GO](https://golang.org/dl/), [protoc](https://github.com/protocolbuffers/protobuf/releases), que será nuestro autogenerador de código a partir de los archivos proto, disponible en el PATH y algún editor que te de un soporte mínimo para archivos GO. Yo uso VS Code y un poco de línea de comandos.

Ve abriendo una línea de comandos en el directorio `cmd`

>Antes de empezar:  
Nuestro cliente GO necesitará que le indiquemos explicitamente el certificado que usa el servidor para exponer el servicio. En net core todo es trasnparente para nosotros puesto que se usan certificados autogenerados que el propio VS o el ejecutable dotnet instalan directamente en el almacen apropiado.  
Para extraer este certificado te propongo los giguientes comandos ejecutados en la carpeta `cmd`  
`dotnet dev-certs https -ep .\ca.der` ➡ para extraer el certificado en formato DER  
`openssl x509 -inform der -in dotnet-ca.cer -outform PEM -out ca.cer` ➡ para convertirlo a Base64  
Si no tienes instalado openssl también puedes abrir la consola de certificados en Windows y exportar el certificado desde ahí.

Ahora sí, comencemos creando nuestro cliente a partir de los proto que ya tenemos generados (en este caso usaremos los de la carpeta `final`). Desde la línea de comandos ejecuta lo siguiente:  
`protoc -I ..\final\WMTServer\Protos\ --go_out=plugins=grpc:.\Windmill  "..\final\WMTServer\Protos\*.proto"`  
La opción -I instruirá al autogenerador para que use la carpeta indicada en los imports.  
--go_out nos servirá para decir donde queremos los archivos de salida. Además podemos complementarlo con plugins, en este caso queremos la generación completa de grpc.  
Finalmente los archivos proto que queremos procesar.

El resulado de este comando será la subcarpeta `Windmill` con 3 archivos terminados en pb.go. Nuestros clientes!

Transformemos esto en un módulo para poderlo usar en la aplicación principal. Navega al directorio desde la línea de comandos y ejecuta:  
`go mod init github.com/expertscoding/AsturiasNetConf-Taller-gRPC/Windmill`  
`go build`  
Si en la línea de comandos no hay ningún mensaje es que todo ha ido bien: _no news are good news_. Ya teemos nuestro módulo. Volvamos a la carpeta superior `cmd`

Crea un archivo main.go con este contenido:

```golang
package main

import (
	"context"
	"log"
	"time"

	pb "github.com/expertscoding/AsturiasNetConf-Taller-gRPC/Windmill"

	"google.golang.org/grpc"
	"google.golang.org/grpc/credentials"
)

const (
	address = "localhost:5001"
)

func main() {
	// Set up a connection to the server.
	creds, err := credentials.NewClientTLSFromFile("ca.cer", "")
	if err != nil {
		log.Fatalf("creds error: %v", err)
	}
	conn, err := grpc.Dial(address, grpc.WithTransportCredentials(creds))
	if err != nil {
		log.Fatalf("did not connect: %v", err)
	}
	defer conn.Close()
	c := pb.NewWindmillFarmClient(conn)

	// Contact the server and print out its response.
	ctx, cancel := context.WithTimeout(context.Background(), time.Second)
	defer cancel()
	r, err := c.RequestList(ctx, &pb.WindmillListRequest{})
	if err != nil {
		log.Fatalf("could not request list: %v", err)
	}

	for _, element := range r.Windmills {
		log.Printf("%s", element)
	}
}
```

Para poder compilar el programa, tendremos que transformar esto igualmente en otro módulo y que la resolución de dependencias vaya bien. En la línea de comandos ejecutamos:  
`go mod init github.com/expertscoding/AsturiasNetConf-Taller-gRPC`  

A continuación editamos el archivo go.mod para añadir al final esta línea:  
`replace github.com/expertscoding/AsturiasNetConf-Taller-gRPC/Windmill => ./Windmill`  
Con esto instruiremos al compilador para que el módulo lo busque en la ruta local a la que estamos redireccionando.

Ahora sí, podemos compilar nuestro porgrama con  
`go build`  
El resultado debe ser un nuevo archivo `AsturiasNetConf-Taller-gRPC.exe` en la misma ruta. Para ejecutarlo asegurate tener iniciado el servidor.

💪 Cliente conseguido. Enhorabuena!

Vamos al siguiente, pongámoselo difícil a los curiosos con un poco de autenticación.

[Siguiente ejercicio >](05-autenticando-el-servicio.md)
