// Entry point for the Respira Media API: configures controllers, OpenAPI,
// Wolverine messaging, EF Core persistence, Cloudflare R2 storage and the media infrastructure.
using Application;
using Infrastructure;
using Respira.ServiceDefaults.Extensions;
using Wolverine;
using Wolverine.EntityFrameworkCore;
using Wolverine.FluentValidation;
using Wolverine.Postgresql;

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
builder.Host.UseWolverine(opts =>
{
    opts.RestoreV5Defaults();
    opts.Discovery.IncludeAssembly(typeof(ApplicationMarker).Assembly);

    opts.PersistMessagesWithPostgresql(conn, "media_db");
    opts.UseEntityFrameworkCoreTransactions();

    opts.UseFluentValidation(RegistrationBehavior.ExplicitRegistration);

    opts.Durability.Mode = DurabilityMode.Solo;
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.ApplyMigrations(app.Environment.IsDevelopment());

app.Run();
