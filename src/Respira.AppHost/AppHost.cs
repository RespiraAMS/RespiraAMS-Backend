var builder = DistributedApplication.CreateBuilder(args);

// Kubernetes settings
var k8s = builder.AddKubernetesEnvironment("k8s");

// Setup databases, messaging and cache
var cache = builder.AddRedis("cache");
var postgres = builder.AddPostgres("postgres").WithPgWeb().WithDataVolume();
var rabbitmq = builder.AddRabbitMQ("rabbitmq").WithManagementPlugin();

var authDb = postgres.AddDatabase("authdb");
var mediaDb = postgres.AddDatabase("mediadb");
var doctorDb = postgres.AddDatabase("doctordb");
var sagaDb = postgres.AddDatabase("sagadb");

var authService = builder
    .AddProject<Projects.Authentication_API>("auth-service")
    .WithReference(authDb)
    .WithReference(rabbitmq)
    .WithReference(cache)
    .WaitFor(authDb)
    .WaitFor(rabbitmq)
    .WaitFor(cache);

var mediaService = builder
    .AddProject<Projects.Media_API>("media-service")
    .WithReference(mediaDb)
    .WithReference(rabbitmq)
    .WithReference(cache)
    .WaitFor(mediaDb)
    .WaitFor(rabbitmq)
    .WaitFor(cache);

var doctorService = builder
    .AddProject<Projects.Respira_Doctor_API>("doctor-service")
    .WithReference(doctorDb)
    .WithReference(rabbitmq)
    .WithReference(cache)
    .WaitFor(doctorDb)
    .WaitFor(rabbitmq)
    .WaitFor(cache);

// The create saga orchestrator: it drives the other services over the broker and uploads
// the avatar straight to the Media API, hence the Media:BaseUrl reference.
var sagaService = builder
    .AddProject<Projects.Respira_Saga_API>("saga-service")
    .WithReference(sagaDb)
    .WithReference(rabbitmq)
    .WithReference(cache)
    .WithEnvironment("Media__BaseUrl", mediaService.GetEndpoint("http"))
    .WaitFor(sagaDb)
    .WaitFor(rabbitmq)
    .WaitFor(cache)
    .WaitFor(mediaService);

builder.Build().Run();
