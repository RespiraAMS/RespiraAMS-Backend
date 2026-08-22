// Entry point for the Respira SagaAudit API: hosts the Wolverine sagas that
// orchestrate cross-service workflows (Auth -> Doctor -> Media) over RabbitMQ.
using Respira.SagaAudit.Application;
using Respira.ServiceDefaults.Extensions;
using Wolverine;
using Wolverine.Postgresql;
using Wolverine.RabbitMQ;

var builder = WebApplication.CreateBuilder(args);

var sagaConn =
    builder.Configuration.GetConnectionString("sagaAuditDb")
    ?? throw new InvalidOperationException("No sagaAuditDb connection string");
var rabbitConn =
    builder.Configuration.GetConnectionString("rabbitmq")
    ?? throw new InvalidOperationException("No rabbitmq connection string");

builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.AddServiceDefaults();

// Add Wolverine as the saga host
builder.Host.UseWolverine(opts =>
{
    opts.Discovery.IncludeAssembly(typeof(SagaAuditApplicationMarker).Assembly);

    // PostgreSQL backs both durable messaging and lightweight saga storage
    opts.PersistMessagesWithPostgresql(sagaConn, "saga_audit_db");

    // RabbitMQ routes every message across service boundaries
    opts.UseRabbitMq(rabbitConn).AutoProvision().UseConventionalRouting();
    opts.Policies.DisableConventionalLocalRouting();

    opts.Durability.Mode = DurabilityMode.Solo;
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
