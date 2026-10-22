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

// Patient service
var patientDb = postgres.AddDatabase("patientDb");
var patientService = builder
    .AddProject<Projects.Respira_Patient_API>("patient-service")
    .WithReference(patientDb)
    .WithReference(cache)
    .WaitFor(cache)
    .WithReference(rabbitmq)
    .WaitFor(rabbitmq);

// Auth service
var authDb = postgres.AddDatabase("authdb");
var authService = builder
    .AddProject<Projects.Respira_Authentication_API>("auth-service")
    .WithReference(authDb)
    .WithReference(rabbitmq)
    .WithReference(cache)
    .WaitFor(authDb)
    .WaitFor(rabbitmq)
    .WaitFor(cache);

// Media service
var mediaDb = postgres.AddDatabase("mediadb");
var mediaService = builder
    .AddProject<Projects.Respira_Media_API>("media-service")
    .WithReference(mediaDb)
    .WithReference(rabbitmq)
    .WithReference(cache)
    .WaitFor(mediaDb)
    .WaitFor(rabbitmq)
    .WaitFor(cache);

// Doctor service
var doctorDb = postgres.AddDatabase("doctordb");
var doctorService = builder
    .AddProject<Projects.Respira_Doctor_API>("doctor-service")
    .WithReference(doctorDb)
    .WithReference(rabbitmq)
    .WithReference(cache)
    .WaitFor(doctorDb)
    .WaitFor(rabbitmq)
    .WaitFor(cache);

// Saga orchestrator
// The create saga orchestrator: it drives the other services over the broker and uploads
// the avatar straight to the Media API, hence the Media:BaseUrl reference.
var sagaDb = postgres.AddDatabase("sagadb");
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

// API gateway
var gateway = builder
    .AddProject<Projects.Respira_Gateway>("gateway")
    .WithReference(clinicalService)
    .WithReference(patientService)
    .WithReference(authService)
    .WithReference(mediaService)
    .WithReference(doctorService)
    .WithReference(sagaService)
    .WithEnvironment("Jwt__Secret", jwtSecret)
    .WithEnvironment("Jwt__Issuer", jwtIssuer)
    .WithEnvironment("Jwt__Audience", jwtAudience)
    .WithExternalHttpEndpoints();
clinicalService.WithReference(gateway).WaitFor(gateway);
patientService.WithReference(gateway).WaitFor(gateway);
authService.WithReference(gateway).WaitFor(gateway);
mediaService.WithReference(gateway).WaitFor(gateway);
doctorService.WithReference(gateway).WaitFor(gateway);
sagaService.WithReference(gateway).WaitFor(gateway);


await builder.Build().RunAsync();
