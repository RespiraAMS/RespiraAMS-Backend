var builder = DistributedApplication.CreateBuilder(args);

// Kubernetes settings
var k8s = builder.AddKubernetesEnvironment("k8s");

// Setup databases, messaging and cache
var cache = builder.AddRedis("cache");
var postgres = builder.AddPostgres("postgres").WithPgWeb().WithDataVolume();
var rabbitmq = builder.AddRabbitMQ("rabbitmq").WithManagementPlugin();
var analyticsDb = postgres.AddDatabase("analytics-db");
var authDb = postgres.AddDatabase("auth-db");
var clinicalDb = postgres.AddDatabase("clinical-db");
var doctorDb = postgres.AddDatabase("doctor-db");
var mediaDb = postgres.AddDatabase("media-db");
var patientDb = postgres.AddDatabase("patient-db");
var sagaAuditDb = postgres.AddDatabase("saga-audit-db");

// Setup services
var analyticsService = builder
    .AddProject<Projects.Respira_Analytics_API>("analytics-service")
    .WithReference(analyticsDb)
    .WithReference(cache)
    .WaitFor(cache)
    .WithReference(rabbitmq)
    .WaitFor(rabbitmq);
var authService = builder
    .AddProject<Projects.Respira_Auth_API>("auth-service")
    .WithReference(authDb)
    .WithReference(cache)
    .WaitFor(cache)
    .WithReference(rabbitmq)
    .WaitFor(rabbitmq);
var clinicalService = builder
    .AddProject<Projects.Respira_Clinical_API>("clinical-service")
    .WithReference(clinicalDb)
    .WithReference(cache)
    .WaitFor(cache)
    .WithReference(rabbitmq)
    .WaitFor(rabbitmq);
var doctorService = builder
    .AddProject<Projects.Respira_Doctor_API>("doctor-service")
    .WithReference(doctorDb)
    .WithReference(cache)
    .WaitFor(cache)
    .WithReference(rabbitmq)
    .WaitFor(rabbitmq);
var mediaService = builder
    .AddProject<Projects.Respira_Media_API>("media-service")
    .WithReference(mediaDb)
    .WithReference(cache)
    .WaitFor(cache)
    .WithReference(rabbitmq)
    .WaitFor(rabbitmq);
var patientService = builder
    .AddProject<Projects.Respira_Patient_API>("patient-service")
    .WithReference(patientDb)
    .WithReference(cache)
    .WaitFor(cache)
    .WithReference(rabbitmq)
    .WaitFor(rabbitmq);
var sagaAuditService = builder
    .AddProject<Projects.Respira_SagaAudit_API>("saga-audit-service")
    .WithReference(sagaAuditDb)
    .WithReference(cache)
    .WaitFor(cache)
    .WithReference(rabbitmq)
    .WaitFor(rabbitmq);
var gateway = builder
    .AddProject<Projects.Respira_Gateway>("gateway")
    .WithReference(analyticsService)
    .WithReference(authService)
    .WithReference(clinicalService)
    .WithReference(doctorService)
    .WithReference(mediaService)
    .WithReference(patientService)
    .WithReference(sagaAuditService)
    .WithExternalHttpEndpoints();

// Make services depend on gateway
analyticsService.WithReference(gateway).WaitFor(gateway);
authService.WithReference(gateway).WaitFor(gateway);
clinicalService.WithReference(gateway).WaitFor(gateway);
doctorService.WithReference(gateway).WaitFor(gateway);
mediaService.WithReference(gateway).WaitFor(gateway);
patientService.WithReference(gateway).WaitFor(gateway);
sagaAuditService.WithReference(gateway).WaitFor(gateway);

// Make services (except Saga)
analyticsService.WithReference(sagaAuditService).WaitFor(sagaAuditService);
authService.WithReference(sagaAuditService).WaitFor(sagaAuditService);
clinicalService.WithReference(sagaAuditService).WaitFor(sagaAuditService);
doctorService.WithReference(sagaAuditService).WaitFor(sagaAuditService);
mediaService.WithReference(sagaAuditService).WaitFor(sagaAuditService);
patientService.WithReference(sagaAuditService).WaitFor(sagaAuditService);

builder.Build().Run();
