using Asp.Versioning;
using Respira.Patient.Application;
using Respira.Patient.DI;
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

    opts.Durability.Mode = DurabilityMode.Balanced;
});

var app = builder.Build();
app.UseCustomErrorHandling();
app.UseClaimsPropagation();
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

await app.RunAsync();
