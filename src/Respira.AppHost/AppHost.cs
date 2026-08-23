var builder = DistributedApplication.CreateBuilder(args);

// Kubernetes settings
var k8s = builder.AddKubernetesEnvironment("k8s");

// Setup databases, messaging and cache
var cache = builder.AddRedis("cache");
var postgres = builder.AddPostgres("postgres").WithPgWeb().WithDataVolume();
var rabbitmq = builder.AddRabbitMQ("rabbitmq").WithManagementPlugin();
var analyticsDb = postgres.AddDatabase("analyticsDb");
var authDb = postgres.AddDatabase("authDb");
var clinicalDb = postgres.AddDatabase("clinicalDb");
var doctorDb = postgres.AddDatabase("doctorDb");
var mediaDb = postgres.AddDatabase("mediaDb");
var patientDb = postgres.AddDatabase("patientDb");
var sagaAuditDb = postgres.AddDatabase("sagaAuditDb");

// Cloudflare R2 (object storage) parameters
var r2Endpoint = builder.AddParameter("r2-endpoint");
var r2AccessKey = builder.AddParameter("r2-access-key");
var r2SecretKey = builder.AddParameter("r2-secret-key");
var r2Bucket = builder.AddParameter("r2-bucket");
var r2PublicUrl = builder.AddParameter("r2-public-url");

// Auth service parameters (values come from AppHost config: Parameters:* section)
var jwtSecret = builder.AddParameter("jwt-secret");
var jwtIssuer = builder.AddParameter("jwt-issuer");
var jwtAudience = builder.AddParameter("jwt-audience");
var smtpHost = builder.AddParameter("smtp-host");
var smtpPort = builder.AddParameter("smtp-port");
var smtpUsername = builder.AddParameter("smtp-username");
var smtpPassword = builder.AddParameter("smtp-password");

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
    .WaitFor(rabbitmq)
    .WithEnvironment("Jwt__Secret", jwtSecret)
    .WithEnvironment("Jwt__Issuer", jwtIssuer)
    .WithEnvironment("Jwt__Audience", jwtAudience)
    .WithEnvironment("Jwt__AccessTokenExpired", "15")
    .WithEnvironment("Jwt__RefreshTokenExpired", "10080")
    .WithEnvironment("Email__Host", smtpHost)
    .WithEnvironment("Email__Port", smtpPort)
    .WithEnvironment("Email__Username", smtpUsername)
    .WithEnvironment("Email__Password", smtpPassword)
    .WithEnvironment("Email__EnableSsl", "true");
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
    .WaitFor(rabbitmq)
    .WithEnvironment("R2__Endpoint", r2Endpoint)
    .WithEnvironment("R2__AccessKey", r2AccessKey)
    .WithEnvironment("R2__SecretKey", r2SecretKey)
    .WithEnvironment("R2__BucketName", r2Bucket)
    .WithEnvironment("R2__PublicBaseUrl", r2PublicUrl);
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
    .WaitFor(rabbitmq)
    .WithReference(mediaService);
var gateway = builder
    .AddProject<Projects.Respira_Gateway>("gateway")
    .WithReference(analyticsService)
    .WithReference(authService)
    .WithReference(clinicalService)
    .WithReference(doctorService)
    .WithReference(mediaService)
    .WithReference(patientService)
    .WithReference(sagaAuditService)
    .WithEnvironment("Jwt__Secret", jwtSecret)
    .WithEnvironment("Jwt__Issuer", jwtIssuer)
    .WithEnvironment("Jwt__Audience", jwtAudience)
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
