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
