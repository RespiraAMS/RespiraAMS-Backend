/// <summary>
/// Entry point for the Respira Doctor API. Configures controllers, OpenAPI,
/// Wolverine messaging, EF Core persistence and the doctor infrastructure (caching + DB).
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
    builder.Configuration.GetConnectionString("doctorDb")
    ?? throw new InvalidOperationException("No connection string found");

builder.Services.AddControllers();
builder.Services.AddOpenApi();

// Add service discovery / telemetry defaults
builder.AddServiceDefaults();

// Add validators
builder.Services.AddFluentValidators();

// Add infrastructure (DB context, FusionCache)
builder.AddInfrastructure();

// Typed HTTP client for calling Auth service (resolved via Aspire Service Discovery).
builder.Services.AddHttpClient<Application.Clients.IAuthClient, Application.Clients.AuthClient>(client =>
{
    client.BaseAddress = new Uri("http://auth-service");
});

// Typed HTTP client for calling Media service (resolved via Aspire Service Discovery).
builder.Services.AddHttpClient<Application.Clients.IMediaClient, Application.Clients.MediaClient>(client =>
{
    client.BaseAddress = new Uri("http://media-service");
});

// Typed HTTP client for calling Media service (resolved via Aspire Service Discovery).
builder.Services.AddHttpClient<Application.Clients.IMediaClient, Application.Clients.MediaClient>(client =>
{
    client.BaseAddress = new Uri("http://media-service");
});

// Add Wolverine
var rabbitConn = builder.Configuration.GetConnectionString("rabbitmq")
    ?? throw new InvalidOperationException("No rabbitmq connection string");

builder.Host.UseWolverine(opts =>
{
    opts.RestoreV5Defaults();
    opts.Discovery.IncludeAssembly(typeof(ApplicationMarker).Assembly);

    opts.PersistMessagesWithPostgresql(conn, "doctor_db");
    opts.UseEntityFrameworkCoreTransactions();

    opts.UseFluentValidation(RegistrationBehavior.ExplicitRegistration);

    // Route messages across service boundaries through RabbitMQ
    opts.UseRabbitMq(rabbitConn).AutoProvision().UseConventionalRouting();
    opts.Policies.DisableConventionalLocalRouting();

    opts.Durability.Mode = DurabilityMode.Solo;
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
