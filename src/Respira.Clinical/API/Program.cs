using Application;
using Asp.Versioning;
using Infrastructure;
using Respira.ServiceDefaults.Extensions;
using Scalar.AspNetCore;
using Wolverine;
using Wolverine.EntityFrameworkCore;
using Wolverine.FluentValidation;
using Wolverine.Postgresql;

var builder = WebApplication.CreateBuilder(args);

// Get connection string
var conn = builder.Configuration.GetConnectionString("clinicalDb");
if (conn is null)
{
    throw new InvalidOperationException("No connection string found");
}

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
builder.Services.AddOpenApiExtension();

// Add error handling
builder.Services.AddCustomErrorHandling();

// Add service discovery
builder.AddServiceDefaults();

// Add mapping profiles
builder.Services.AddProfiles();

// Add validators
builder.Services.AddFluentValidators();

// Add infrastructure
builder.AddInfrastructure();

// Add Wolverine
builder.Host.UseWolverine(opts =>
{
    opts.RestoreV5Defaults();
    opts.Discovery.IncludeAssembly(typeof(ApplicationMarker).Assembly);

    opts.PersistMessagesWithPostgresql(conn, "clinical_db");
    opts.UseEntityFrameworkCoreTransactions();

    opts.UseFluentValidation(RegistrationBehavior.ExplicitRegistration);

    opts.Durability.Mode = DurabilityMode.Solo;
});

var app = builder.Build();

app.UseCustomErrorHandling();
app.MapControllers();
// app.UseClaimsPropagation();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(opts => { opts.Theme = ScalarTheme.Kepler; });
}

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.ApplyMigrations(app.Environment.IsDevelopment());

app.Run();