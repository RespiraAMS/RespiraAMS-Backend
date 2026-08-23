/// <summary>
/// Entry point for the Respira Media API. Wires up controllers, OpenAPI/Scalar,
/// Wolverine messaging (PostgreSQL durability + RabbitMQ routing), EF Core persistence,
/// Cloudflare R2 storage and the media infrastructure, and exposes the upload/read endpoints.
/// </summary>
using Application;
using Infrastructure;
using Respira.ServiceDefaults.Extensions;
using Wolverine;
using Wolverine.EntityFrameworkCore;
using Wolverine.FluentValidation;
using Wolverine.Postgresql;
using Wolverine.RabbitMQ;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Get connection string
var conn =
    builder.Configuration.GetConnectionString("mediaDb")
    ?? throw new InvalidOperationException("No connection string found");

builder.Services.AddControllers();
builder.Services.AddOpenApi();

// Add service discovery / telemetry defaults
builder.AddServiceDefaults();

// Add validators
builder.Services.AddFluentValidators();

// Add infrastructure (DB context, R2 storage)
builder.AddInfrastructure();

// Add Wolverine
var rabbitConn = builder.Configuration.GetConnectionString("rabbitmq")
    ?? throw new InvalidOperationException("No rabbitmq connection string");

builder.Host.UseWolverine(opts =>
{
    opts.RestoreV5Defaults();
    opts.Discovery.IncludeAssembly(typeof(ApplicationMarker).Assembly);

    opts.PersistMessagesWithPostgresql(conn, "media_db");
    opts.UseEntityFrameworkCoreTransactions();

    opts.UseFluentValidation(RegistrationBehavior.ExplicitRegistration);

    // Route messages across service boundaries through RabbitMQ
    opts.UseRabbitMq(rabbitConn).AutoProvision().UseConventionalRouting();
    opts.Policies.DisableConventionalLocalRouting();

    opts.Durability.Mode = DurabilityMode.Solo;
});

var app = builder.Build();

// Configure the HTTP request pipeline.
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
