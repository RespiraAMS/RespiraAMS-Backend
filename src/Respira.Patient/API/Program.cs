using Respira.Patient.API.Middlewares;
using Respira.Patient.API.Auditing;
using Respira.Patient.Application;
using Respira.Patient.Application.Contracts.Messages;
using Respira.Patient.Application.Features.TreatmentPlans.UpsertTreatmentPlan;
using Respira.Patient.DI;
using Respira.Patient.Infrastructure.Data;
using Respira.ServiceDefaults.Auditing;
using Respira.ServiceDefaults.Contracts.Messages.Audit;
using Respira.ServiceDefaults.Extensions;
using Wolverine;
using Wolverine.EntityFrameworkCore;
using Wolverine.FluentValidation;
using Wolverine.Postgresql;
using Wolverine.RabbitMQ;

var builder = WebApplication.CreateBuilder(args);

// Get connection string
var conn = builder.Configuration.GetConnectionString("patientDb") ?? throw new InvalidOperationException("No connection string found");

// Add API controllers
builder.Services.AddControllers().AddJsonOptions(options => options.JsonSerializerOptions.PropertyNameCaseInsensitive = true);

// Add API versioning
builder.Services.AddApiVersion();

// Add error handling
builder.Services.AddCustomErrorHandling();

// Add service discovery
builder.AddServiceDefaults();

// DI registration
builder.AddDI();
builder.Services.AddAuditLogging();
builder.Services.AddScoped<IAuditEventPublisher, PatientAuditEventPublisher>();
builder.Services.AddHostedService<AuditOutboxForwarder<PatientDbContext>>();

// Add Wolverine
builder.Host.UseWolverine(opts =>
{
    opts.RestoreV5Defaults();
    opts.Discovery.IncludeAssembly(typeof(ApplicationMarker).Assembly);

    opts.PersistMessagesWithPostgresql(conn, "patient_db");
    opts.UseEntityFrameworkCoreTransactions();

    opts.UseFluentValidation(RegistrationBehavior.ExplicitRegistration);

    // Setup queue
    opts.UseRabbitMqUsingNamedConnection("rabbitmq").AutoProvision();
    opts.ListenToRabbitQueue(Queues.DiagnoseQueue);
    opts.ListenToRabbitQueue(Queues.AnalysisQueries);
    opts.PublishMessage<UpsertTreatmentPlanResult>().ToRabbitQueue(Queues.DiagnoseQueue);
    opts.PublishMessage<AuditEvent>().ToRabbitQueue(AuditQueues.Events);

    opts.Durability.Mode = DurabilityMode.Balanced;
});

var app = builder.Build();
app.UseCustomErrorHandling();
app.UseClaimsPropagation();
app.UseAuthorizationMiddleware();
app.MapControllers();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseApiDocumentation();
}

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}
app.ApplyMigrations(app.Environment.IsDevelopment());

if (app.Environment.IsDevelopment())
{
    await app.SeedAnalysisDataAsync();
}

await app.RunAsync();
