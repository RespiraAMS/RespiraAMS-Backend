using Respira.ServiceDefaults.Extensions;
using Wolverine;
using Wolverine.Postgresql;
using Wolverine.EntityFrameworkCore;
using Wolverine.FluentValidation;
using Wolverine.RabbitMQ;
using Respira.Clinical.Application;
using Respira.Clinical.DI;
using System.Text.Json.Serialization;
using ZiggyCreatures.Caching.Fusion;
using System.Text.Json;
using Respira.Clinical.Application.Contracts.Messages;
using Respira.Clinical.Application.Features.Diagnosis.ConfirmTreatment;

var builder = WebApplication.CreateBuilder(args);

// Get connection string
var conn = builder.Configuration.GetConnectionString("clinicalDb") ?? throw new InvalidOperationException("No connection string found");

// Add API controllers
builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
    options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
});

// Add API versioning
builder.Services.AddApiVersion();

// Add error handling
builder.Services.AddCustomErrorHandling();

// Add service discovery
builder.AddServiceDefaults();

// DI registration
builder.AddDI();

// Add caching
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("cache");
    options.InstanceName = "respira-clinical:";
});
builder.Services
    .AddFusionCache()
    .WithSystemTextJsonSerializer(new JsonSerializerOptions
    {
        IncludeFields = true
    })
    .WithRegisteredDistributedCache()
    .WithDefaultEntryOptions(options =>
    {
        options.Duration = TimeSpan.FromMinutes(2);
        // Note that, since failed safe in enable, the actual
        // TTL would be default to fusion cache value (1 day).
        // The expiration time would still work, but just logically,
        // the data still sit there
        options.IsFailSafeEnabled = true;
    });

// Add Wolverine
builder.Host.UseWolverine(opts =>
{
    opts.RestoreV5Defaults();
    opts.Discovery.IncludeAssembly(typeof(ApplicationMarker).Assembly);

    opts.PersistMessagesWithPostgresql(conn, "clinical_db");
    opts.UseEntityFrameworkCoreTransactions();

    opts.UseFluentValidation(RegistrationBehavior.ExplicitRegistration);

    // Setup queue
    opts.UseRabbitMqUsingNamedConnection("rabbitmq").AutoProvision();

    opts.ListenToRabbitQueue(Queues.DiagnoseQueue);
    opts.PublishMessage<UpsertTreatmentPlanCommand>().ToRabbitQueue(Queues.DiagnoseQueue);

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
await app.SeedData();

await app.RunAsync();
