using Asp.Versioning;
using Asp.Versioning.ApiExplorer;
using Media.API.Extensions;
using Media.Application.Constracts.Messaging;
using Media.Application.Constracts.Storage;
using Media.Application.Features.MediaAssets.Create;
using Media.Infrastructure.Extensions;
using Media.Infrastructure.Persistence;
using Respira.ServiceDefaults.Contracts.Messages.Audit;
using Wolverine;
using Wolverine.Postgresql;
using Wolverine.RabbitMQ;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddOptions<R2Options>()
    .BindConfiguration(R2Options.SectionName)
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddControllers();
builder.Services
    .AddApiVersioning(options =>
    {
        options.DefaultApiVersion = new ApiVersion(1.0);
        options.ReportApiVersions = true;
        options.ApiVersionReader = new UrlSegmentApiVersionReader();
    })
    .AddMvc()
    .AddApiExplorer(options =>
    {
        options.GroupNameFormat = "'v'VVV";
        options.SubstituteApiVersionInUrl = true;
    })
    .AddOpenApi();

builder.AddNpgsqlDbContext<MediaDbContext>("mediadb");
builder.Services.AddMediaInfrastructure();

// Discover command/query handlers in the Media.Application assembly (e.g. CreateMediaAssetCommandHandler)
builder.Host.UseWolverine(options =>
{
    options.UseRuntimeCompilation();
    options.Discovery.IncludeAssembly(typeof(CreateMediaAssetCommand).Assembly);

    options
        .UseRabbitMq(
            builder.Configuration.GetConnectionString("rabbitmq")
                ?? throw new InvalidOperationException(
                    "Connection string 'rabbitmq' is not configured"
                )
        )
        .AutoProvision();

    options.PersistMessagesWithPostgresql(
        builder.Configuration.GetConnectionString("mediadb")
            ?? throw new InvalidOperationException("Connection string 'mediadb' is not configured")
    );

    // Inbound: media asset lookups requested by other microservices (e.g. Doctor search).
    options.ListenToRabbitQueue(RabbitQueues.MediaQueries);
    options.PublishMessage<AuditEvent>().ToRabbitQueue(AuditQueues.Events);
});

var app = builder.Build();

await app.ApplyMigrationsAsync();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi().WithDocumentPerVersion();
}

app.UseHttpsRedirection();

app.MapControllers();

app.Run();
