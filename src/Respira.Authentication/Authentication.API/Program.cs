using Authentication.API.Extensions;
using Asp.Versioning;
using Authentication.Application.Constracts.Authentication;
using Authentication.Application.Constracts.Data;
using Authentication.Application.Constracts.Email;
using Authentication.Application.Constracts.Messaging;
using Authentication.Application.Features.Accounts.Commands.Create.Events;
using Authentication.Application.Features.Accounts.Commands.SoftDelete.Events;
using Authentication.Application.Features.Accounts.Commands.Update.Events;
using Authentication.Application.Features.Authentication.Commands.Login;
using Authentication.Application.Features.Authentication.Commands.SendVerify.Events;
using Authentication.Infrastructure.Extensions;
using Authentication.Infrastructure.Persistence;
using Wolverine;
using Wolverine.Postgresql;
using Wolverine.RabbitMQ;
using ZiggyCreatures.Caching.Fusion;

var builder = WebApplication.CreateBuilder(args);

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
builder.Services
    .AddOptions<JwtOption>()
    .BindConfiguration("Jwt")
    .ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services
    .AddOptions<EmailOption>()
    .BindConfiguration(EmailOption.SectionName)
    .ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services
    .AddOptions<EmailVerificationOption>()
    .BindConfiguration(EmailVerificationOption.SectionName)
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.AddNpgsqlDbContext<AuthDbContext>("authdb");
builder.Services.AddScoped<IAuthDbContext>(serviceProvider =>
    serviceProvider.GetRequiredService<AuthDbContext>()
);
builder.AddRedisClient("cache");
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("cache");
    options.InstanceName = "respira-auth:";
});
builder.Services
    .AddFusionCache()
    .WithSystemTextJsonSerializer()
    .WithRegisteredDistributedCache()
    .WithDefaultEntryOptions(options =>
    {
        options.Duration = TimeSpan.FromMinutes(2);
        options.IsFailSafeEnabled = true;
    });
builder.Services.AddAuthenticationInfrastructure();

builder.Host.UseWolverine(options =>
{
    options.UseRuntimeCompilation();
    options.Discovery.IncludeAssembly(typeof(LoginCommand).Assembly);

    // Durable inbox/outbox on PostgreSQL: incoming saga commands survive a restart and
    // are retried instead of being lost with the process.
    options.PersistMessagesWithPostgresql(
        builder.Configuration.GetConnectionString("authdb")
            ?? throw new InvalidOperationException(
                "Connection string 'authdb' is not configured"
            )
    );

    options.UseRabbitMqUsingNamedConnection("rabbitmq").AutoProvision();

    // Inbound: commands dispatched by the create saga.
    options.ListenToRabbitQueue(RabbitQueues.AuthCommands);

    // Outbound: saga progress events, all routed to the saga's inbox queue.
    options
        .PublishMessage<CreateAccountCompleted>()
        .ToRabbitQueue(RabbitQueues.SagaEvents);
    options
        .PublishMessage<CreateAccountFailure>()
        .ToRabbitQueue(RabbitQueues.SagaEvents);
    options.PublishMessage<SendVerifyCompleted>().ToRabbitQueue(RabbitQueues.SagaEvents);
    options.PublishMessage<SendVerifyFailure>().ToRabbitQueue(RabbitQueues.SagaEvents);
    options
        .PublishMessage<UpdateAccountCompleted>()
        .ToRabbitQueue(RabbitQueues.SagaEvents);
    options
        .PublishMessage<UpdateAccountFailure>()
        .ToRabbitQueue(RabbitQueues.SagaEvents);
    options
        .PublishMessage<SoftDeleteCompleted>()
        .ToRabbitQueue(RabbitQueues.SagaEvents);
    options.PublishMessage<SoftDeleteFailure>().ToRabbitQueue(RabbitQueues.SagaEvents);
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

await app.RunAsync();
