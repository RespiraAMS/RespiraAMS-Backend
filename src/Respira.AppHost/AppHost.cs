var builder = DistributedApplication.CreateBuilder(args);

// Kubernetes settings
var k8s = builder.AddKubernetesEnvironment("k8s");

// Setup databases, messaging and cache
var cache = builder.AddRedis("cache");
var postgres = builder.AddPostgres("postgres").WithPgWeb().WithDataVolume();
var rabbitmq = builder.AddRabbitMQ("rabbitmq").WithManagementPlugin();

// Auth service parameters (values come from AppHost config: Parameters:* section)
var jwtSecret = builder.AddParameter("jwt-secret");
var jwtIssuer = builder.AddParameter("jwt-issuer");
var jwtAudience = builder.AddParameter("jwt-audience");

// Clinical service
var clinicalDb = postgres.AddDatabase("clinicalDb");
var clinicalService = builder
    .AddProject<Projects.Respira_Clinical_API>("clinical-service")
    .WithReference(clinicalDb)
    .WithReference(cache)
    .WaitFor(cache)
    .WithReference(rabbitmq)
    .WaitFor(rabbitmq);

// API gateway
var gateway = builder
    .AddProject<Projects.Respira_Gateway>("gateway")
    .WithReference(clinicalService)
    .WithEnvironment("Jwt__Secret", jwtSecret)
    .WithEnvironment("Jwt__Issuer", jwtIssuer)
    .WithEnvironment("Jwt__Audience", jwtAudience)
    .WithExternalHttpEndpoints();
clinicalService.WithReference(gateway).WaitFor(gateway);

builder.Build().Run();
