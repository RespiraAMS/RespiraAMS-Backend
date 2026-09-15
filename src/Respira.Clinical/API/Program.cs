using Asp.Versioning;
using Respira.ServiceDefaults.Extensions;
using Respira.ServiceDefaults.Utils.OpenApiTransformers;
using Respira.DI;
using Wolverine;
using Wolverine.Postgresql;
using Wolverine.EntityFrameworkCore;
using Wolverine.FluentValidation;
using Wolverine.RabbitMQ;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Get connection string
var conn = builder.Configuration.GetConnectionString("clinicalDb") ?? throw new InvalidOperationException("No connection string found");

// Add API controllers
builder.Services.AddControllers();

// Add API versioning
builder.Services.AddApiVersioning(options =>
{
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
    options.ApiVersionReader = new UrlSegmentApiVersionReader();
}).AddMvc();

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

// Add mapping profiles
// builder.Services.AddProfiles();

// Add validators
// builder.Services.AddFluentValidators();

// Add domain services
// builder.Services.AddServices();

// Add infrastructure
builder.AddInfrastructure();

// Add Wolverine
builder.Host.UseWolverine(opts =>
{
    opts.RestoreV5Defaults();

    opts.PersistMessagesWithPostgresql(conn, "clinical_db");
    opts.UseEntityFrameworkCoreTransactions();

    opts.UseFluentValidation(RegistrationBehavior.ExplicitRegistration);

    // Setup queue
    opts.UseRabbitMqUsingNamedConnection("rabbitmq").AutoProvision();

    opts.Durability.Mode = DurabilityMode.Balanced;
});

var app = builder.Build();

app.UseCors("test");
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
await app.SeedData();

app.Run();
