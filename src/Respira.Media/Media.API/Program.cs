using Respira.ServiceDefaults.Contracts.Messages.Audit;
using Respira.Media.API.Extensions;
using Respira.ServiceDefaults.Extensions;
using Wolverine;
using Wolverine.Postgresql;
using Respira.Media.Infrastructure.Persistence;
using Respira.Media.Infrastructure.Extensions;
using Respira.Media.Application.Features.MediaAssets.Create;
using Respira.Media.Application.Constracts.Storage;
using Wolverine.RabbitMQ;
using Respira.Media.Application.Contracts.Messaging;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddOptions<R2Options>()
    .BindConfiguration(R2Options.SectionName)
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddControllers();
builder.Services.AddApiVersion();

builder.AddNpgsqlDbContext<MediaDbContext>("mediadb");
builder.Services.AddMediaInfrastructure();

// Discover command/query handlers in the Media.Application assembly (e.g. CreateMediaAssetCommandHandler)
builder.Host.UseWolverine(options =>
{
    options.UseRuntimeCompilation();
    options.Discovery.IncludeAssembly(typeof(CreateMediaAssetCommand).Assembly);

    var mqconn = builder.Configuration.GetConnectionString("rabbitmq")
        ?? throw new InvalidOperationException("Connection string 'rabbitmq' is not configured");
    options.UseRabbitMq(mqconn).AutoProvision();

    var conn = builder.Configuration.GetConnectionString("mediadb")
        ?? throw new InvalidOperationException("Connection string 'mediadb' is not configured");
    options.PersistMessagesWithPostgresql(conn);

    // Inbound: media asset lookups requested by other microservices (e.g. Doctor search).
    options.ListenToRabbitQueue(RabbitQueues.MediaQueries);
    options.PublishMessage<AuditEvent>().ToRabbitQueue(AuditQueues.Events);
});

var app = builder.Build();

await app.ApplyMigrationsAsync();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseApiDocumentation();
}

app.UseHttpsRedirection();

app.MapControllers();

await app.RunAsync();
