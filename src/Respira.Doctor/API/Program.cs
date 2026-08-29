/// <summary>
/// Entry point for the Respira Doctor API. Configures controllers, OpenAPI,
/// Wolverine messaging, EF Core persistence and the doctor infrastructure (caching + DB).
/// </summary>
using Application;
using Asp.Versioning;
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
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
    options.ApiVersionReader = new UrlSegmentApiVersionReader();
}).AddMvc();
builder.Services.AddOpenApi();

// Add service discovery / telemetry defaults
builder.AddServiceDefaults();

// Add validators
builder.Services.AddFluentValidators();

// Add infrastructure (DB context, FusionCache)
builder.AddInfrastructure();

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
