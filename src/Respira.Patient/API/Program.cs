using Application;
using Application.Features.Treatments.CreateTreatment;
using Asp.Versioning;
using Infrastructure;
using Respira.Patient.API.Converters;
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
    builder.Configuration.GetConnectionString("patientDb")
    ?? throw new InvalidOperationException("No connection string found");

// Add API controllers
builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new DiagnosisRecordJsonConverter()));

// Add API versioning
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
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

    opts.PersistMessagesWithPostgresql(conn, "patient_db");
    opts.UseEntityFrameworkCoreTransactions();

    opts.UseFluentValidation(RegistrationBehavior.ExplicitRegistration);

    // Setup queue
    opts.UseRabbitMqUsingNamedConnection("rabbitmq").AutoProvision();

    opts.ListenToRabbitQueue("validate-diagnosis-result-queue");
    opts.PublishMessage<ValidateDiagnosisQuery>().ToRabbitQueue("validate-diagnosis-query-queue");

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
    app.MapScalarApiReference(opts => opts.Theme = ScalarTheme.Kepler);
}

app.UseHttpsRedirection();
app.UseClaimsPropagation();
app.UseAuthorization();
app.MapControllers();

app.Run();
