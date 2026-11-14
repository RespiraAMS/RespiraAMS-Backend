using Respira.AppHost;

var builder = DistributedApplication.CreateBuilder(args);

// Kubernetes settings
var k8s = builder.AddKubernetesEnvironment("k8s");

// Setup databases, messaging and cache
var cache = builder.AddRedis("cache").WithRedisInsight();
var postgres = builder.AddPostgres("postgres").WithPgWeb().WithPgAdmin().WithDataVolume();
var rabbitmq = builder.AddRabbitMQ("rabbitmq").WithManagementPlugin();

// Shared configuration
// Every setting the services share lives in the AppHost configuration
// (src/Respira.AppHost/appsettings.json) and is handed over to the services as
// environment variables. The secrets are parameters, so every developer can replace
// them without touching the repository:
//   dotnet user-secrets set "Parameters:jwt-secret" "<your secret>"
// (run in the src/Respira.AppHost folder).
var shared = builder.Configuration.GetSection("Shared");
var jwtSecret = builder.AddParameter("jwt-secret", secret: true);
var emailUserName = builder.AddParameter("email-user-name", secret: true);
var emailPassword = builder.AddParameter("email-password", secret: true);
var r2AccessKey = builder.AddParameter("r2-access-key", secret: true);
var r2SecretKey = builder.AddParameter("r2-secret-key", secret: true);

// Clinical service
var clinicalDb = postgres.AddDatabase("clinicalDb");
var clinicalService = builder
    .AddProject<Projects.Respira_Clinical_API>("clinical-service")
    .WithReference(clinicalDb)
    .WithReference(cache)
    .WaitFor(cache)
    .WithReference(rabbitmq)
    .WaitFor(rabbitmq)
    .WithSharedConfiguration(shared, "ClinicalSeedData", "SeedData");

// Patient service
var patientDb = postgres.AddDatabase("patientDb");
var patientService = builder
    .AddProject<Projects.Respira_Patient_API>("patient-service")
    .WithReference(patientDb)
    .WithReference(cache)
    .WaitFor(cache)
    .WithReference(rabbitmq)
    .WaitFor(rabbitmq);

// Analysis reports persist immutable summaries and query closed treatment plans from Patient.
var analysisDb = postgres.AddDatabase("analysisdb");
var analysisService = builder
    .AddProject<Projects.Respira_Analysis_API>("analysis-service")
    .WithReference(analysisDb)
    .WithReference(rabbitmq)
    .WithEnvironment("Jwt__Secret", jwtSecret)
    .WithSharedConfiguration(shared, "Jwt")
    .WaitFor(analysisDb)
    .WaitFor(rabbitmq);

// Auth service
var authDb = postgres.AddDatabase("authdb");
var authService = builder
    .AddProject<Projects.Respira_Authentication_API>("auth-service")
    .WithReference(authDb)
    .WithReference(rabbitmq)
    .WithReference(cache)
    .WithEnvironment("Jwt__Secret", jwtSecret)
    .WithEnvironment("Email__UserName", emailUserName)
    .WithEnvironment("Email__Password", emailPassword)
    .WithSharedConfiguration(shared, "Jwt")
    .WithSharedConfiguration(shared, "Email")
    .WithSharedConfiguration(shared, "EmailVerification")
    .WithSharedConfiguration(shared, "PasswordReset")
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
    .WithEnvironment("R2__AccessKey", r2AccessKey)
    .WithEnvironment("R2__SecretKey", r2SecretKey)
    .WithSharedConfiguration(shared, "R2")
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
    .WithEnvironment("Jwt__Secret", jwtSecret)
    .WithSharedConfiguration(shared, "Jwt")
    .WaitFor(sagaDb)
    .WaitFor(rabbitmq)
    .WaitFor(cache)
    .WaitFor(mediaService);

// API gateway
var gateway = builder
    .AddProject<Projects.Respira_Gateway>("gateway")
    .WithReference(clinicalService)
    .WithReference(patientService)
    .WithReference(analysisService)
    .WithReference(authService)
    .WithReference(mediaService)
    .WithReference(doctorService)
    .WithReference(sagaService)
    .WithEnvironment("Jwt__Secret", jwtSecret)
    .WithSharedConfiguration(shared, "Jwt")
    .WithExternalHttpEndpoints();
clinicalService.WithReference(gateway).WaitFor(gateway);
patientService.WithReference(gateway).WaitFor(gateway);
analysisService.WithReference(gateway).WaitFor(gateway);
authService.WithReference(gateway).WaitFor(gateway);
mediaService.WithReference(gateway).WaitFor(gateway);
doctorService.WithReference(gateway).WaitFor(gateway);
sagaService.WithReference(gateway).WaitFor(gateway);

await builder.Build().RunAsync();
