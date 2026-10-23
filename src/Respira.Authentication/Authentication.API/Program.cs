using Wolverine;
using Wolverine.Postgresql;
using Wolverine.RabbitMQ;
using ZiggyCreatures.Caching.Fusion;
using Respira.ServiceDefaults.Extensions;
using Respira.Authentication.API.Extensions;
using Respira.Authentication.Application.Constracts.Authentication;
using Respira.Authentication.Application.Constracts.Data;
using Respira.Authentication.Application.Constracts.Email;
using Respira.Authentication.Application.Constracts.Messaging;
using Respira.Authentication.Application.Features.Accounts.Commands.Create.Events;
using Respira.Authentication.Application.Features.Accounts.Commands.SoftDelete.Events;
using Respira.Authentication.Application.Features.Accounts.Commands.Update.Events;
using Respira.Authentication.Application.Features.Authentication.Commands.Login;
using Respira.Authentication.Application.Features.Authentication.Commands.SendVerify.Events;
using Respira.Authentication.Infrastructure.Extensions;
using Respira.Authentication.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddApiVersion();
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
    var conn = builder.Configuration.GetConnectionString("authdb")
        ?? throw new InvalidOperationException("Connection string 'authdb' is not configured");
    options.PersistMessagesWithPostgresql(conn);

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
    app.UseApiDocumentation();
}

app.UseHttpsRedirection();
app.MapControllers();

await app.RunAsync();
