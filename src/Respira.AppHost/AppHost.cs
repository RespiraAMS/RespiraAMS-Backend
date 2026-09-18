var builder = DistributedApplication.CreateBuilder(args);

// Kubernetes settings
var k8s = builder.AddKubernetesEnvironment("k8s");

// Setup databases, messaging and cache
var cache = builder.AddRedis("cache");
var postgres = builder.AddPostgres("postgres").WithPgWeb().WithDataVolume();
var rabbitmq = builder.AddRabbitMQ("rabbitmq").WithManagementPlugin();

var authDb = postgres.AddDatabase("authdb");
var mediaDb = postgres.AddDatabase("mediadb");

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

builder.Build().Run();
