// Entry point for the Respira Auth API: configures controllers, OpenAPI,
// Wolverine messaging, EF Core persistence and the auth infrastructure.
using Application;
using Respira.Auth.API.BackgroundServices;
using Application.Abstracts.Email;
using Asp.Versioning;
using Infrastructure;
using Respira.ServiceDefaults.Extensions;
using Respira.ServiceDefaults.Utils.OpenApiTransformers;
using Scalar.AspNetCore;
using Wolverine;
using Wolverine.EntityFrameworkCore;
using Wolverine.FluentValidation;
using Wolverine.Postgresql;
using Wolverine.RabbitMQ;

var builder = WebApplication.CreateBuilder(args);

// Get connection string
var conn =
    builder.Configuration.GetConnectionString("authDb")
    ?? throw new InvalidOperationException("No connection string found");

// Add API controllers
builder.Services.AddControllers();

// Add API versioning
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
});

// Add OpenAPI support
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer<CustomDocumentTransformer>();
    options.AddSchemaTransformer<CustomSchemaTransformer>();
});

// Add error handling
builder.Services.AddCustomErrorHandling();

// Add service discovery
builder.AddServiceDefaults();

// Add validators
builder.Services.AddFluentValidators();

// Add infrastructure
builder.AddInfrastructure();

// Add Wolverine
builder.Services.AddHostedService<TokenCleanupBackgroundService>();

var rabbitConn = builder.Configuration.GetConnectionString("rabbitmq")
    ?? throw new InvalidOperationException("No rabbitmq connection string");

builder.Host.UseWolverine(opts =>
{
    opts.RestoreV5Defaults();
    opts.Discovery.IncludeAssembly(typeof(ApplicationMarker).Assembly);

    opts.PersistMessagesWithPostgresql(conn, "auth_db");
    opts.UseEntityFrameworkCoreTransactions();

    opts.UseFluentValidation(RegistrationBehavior.ExplicitRegistration);

    // Route messages across service boundaries through RabbitMQ
    opts.UseRabbitMq(rabbitConn).AutoProvision().UseConventionalRouting();
    opts.Policies.DisableConventionalLocalRouting();

    opts.Durability.Mode = DurabilityMode.Solo;
});

var app = builder.Build();

app.UseCustomErrorHandling();
app.UseClaimsPropagation();
app.MapControllers();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(opts => opts.Theme = ScalarTheme.Kepler);
}

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.ApplyMigrations(app.Environment.IsDevelopment());

app.Run();
