/// <summary>
/// Entry point for the Respira SagaAudit API. This microservice owns the saga
/// orchestration for doctor lifecycle workflows (Create/Update/Delete across the
/// Auth, Doctor and Media services over RabbitMQ) and exposes the audit/process
/// tracking endpoints. This file wires up controllers, the Media upload HTTP
/// client, infrastructure (DB context + ProcessTracker), and the Wolverine
/// saga host (PostgreSQL durable messaging + RabbitMQ routing).
/// </summary>
// Entry point for the Respira SagaAudit API: hosts the Wolverine sagas that
// orchestrate cross-service workflows (Auth -> Doctor -> Media) over RabbitMQ.
using Respira.SagaAudit.Application;
using Respira.SagaAudit.Infrastructure;
using Respira.ServiceDefaults.Extensions;
using Wolverine;
using Wolverine.Postgresql;
using Wolverine.RabbitMQ;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

var sagaConn =
    builder.Configuration.GetConnectionString("sagaAuditDb")
    ?? throw new InvalidOperationException("No sagaAuditDb connection string");
var rabbitConn =
    builder.Configuration.GetConnectionString("rabbitmq")
    ?? throw new InvalidOperationException("No rabbitmq connection string");

builder.Services.AddControllers();
builder.Services.AddOpenApi();

// Typed client used by the Create doctor endpoint to pre-upload the avatar to the
// Media service (resolved via Aspire service discovery configured in AddServiceDefaults).
builder.Services.AddHttpClient<Respira.SagaAudit.API.Clients.MediaUploadClient>(client =>
{
    client.BaseAddress = new Uri("http://media-service");
});

builder.AddServiceDefaults();

// Add infrastructure (DB context, ProcessTracker)
builder.AddInfrastructure();

// Add Wolverine as the saga host
builder.Host.UseWolverine(opts =>
{
    opts.Discovery.IncludeAssembly(typeof(SagaAuditApplicationMarker).Assembly);

    // PostgreSQL backs both durable messaging and lightweight saga storage
    opts.PersistMessagesWithPostgresql(sagaConn, "saga_audit_db");

    // RabbitMQ routes every message across service boundaries
    opts.UseRabbitMq(rabbitConn).AutoProvision().UseConventionalRouting();
    opts.Policies.DisableConventionalLocalRouting();

    opts.Durability.Mode = DurabilityMode.Balanced;
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(opts => opts.Theme = ScalarTheme.Kepler);
}

app.UseHttpsRedirection();
app.UseClaimsPropagation();
app.UseAuthorization();
app.MapControllers();

app.ApplyMigrations(app.Environment.IsDevelopment());

app.Run();
