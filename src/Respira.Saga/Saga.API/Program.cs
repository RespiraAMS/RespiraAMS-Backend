using System.Text;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Respira.Saga.Application.Contracts.Data;
using Respira.Saga.Application.Contracts.Messages.Authentication;
using Respira.Saga.Application.Contracts.Messages.Authentication.Create;
using Respira.Saga.Application.Contracts.Messages.Authentication.SoftDelete;
using Respira.Saga.Application.Contracts.Messages.Authentication.Update;
using Respira.Saga.Application.Contracts.Messages.Doctor.Create;
using Respira.Saga.Application.Contracts.Messages.Doctor.SoftDelete;
using Respira.Saga.Application.Contracts.Messages.Doctor.Update;
using Respira.Saga.Application.Contracts.Messaging;
using Respira.Saga.Application.Features.Audit.Queries.Search;
using Respira.Saga.Application.Features.Create.Commands;
using Respira.Saga.Infrastructure.Extensions;
using Respira.Saga.Infrastructure.Persistence;
using Respira.ServiceDefaults.Contracts.Messages.Audit;
using Respira.ServiceDefaults.Contracts.Messages.Doctors;
using Respira.ServiceDefaults.Extensions;
using Wolverine;
using Wolverine.FluentValidation;
using Wolverine.Postgresql;
using Wolverine.RabbitMQ;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
var jwtSecret = builder.Configuration["Jwt:Secret"]
    ?? throw new InvalidOperationException("Jwt:Secret is not configured");
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "Respira",
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"] ?? "Respira",
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,
        };
    });
builder.Services.AddAuthorization();
builder.AddNpgsqlDbContext<SagaAuditDbContext>("sagadb");
builder.Services.AddScoped<ISagaAuditDbContext>(services => services.GetRequiredService<SagaAuditDbContext>());
builder.Services.AddValidatorsFromAssemblyContaining<SearchSagaAuditQuery>();
builder.Services.AddApiVersion();

// The avatar is uploaded to the Media service over HTTP, every other step travels
// over RabbitMQ.
builder.Services.AddSagaInfrastructure(builder.Configuration);

// This service is the orchestrator: it owns the saga state and publishes commands to
// the other microservices while listening to their completion events.
builder.Host.UseWolverine(options =>
{
    options.UseRuntimeCompilation();

    // Discover the sagas and the status query handlers in the Saga.Application assembly.
    options.Discovery.IncludeAssembly(typeof(StartCreateCommand).Assembly);
    options.UseFluentValidation(RegistrationBehavior.ExplicitRegistration);

    // The soft delete events are handled by CreateSaga (compensation) and by
    // DeleteSaga (the delete flow): keep the chains apart so each saga gets its own and
    // can ignore the ids that belong to the other one.
    options.MultipleHandlerBehavior = MultipleHandlerBehavior.Separated;

    // Durable inbox/outbox on PostgreSQL, which also persists the saga state itself.
    options.PersistMessagesWithPostgresql(
        builder.Configuration.GetConnectionString("sagadb")
            ?? throw new InvalidOperationException("Connection string 'sagadb' is not configured")
    );

    var mqconn = builder.Configuration.GetConnectionString("rabbitmq")
        ?? throw new InvalidOperationException("Connection string 'rabbitmq' is not configured");
    options.UseRabbitMq(mqconn).AutoProvision();

    // Inbound: completion and failure events raised by Authentication and Doctor.
    options.ListenToRabbitQueue(RabbitQueues.SagaEvents);
    // Inbound audit events from every service are persisted by SagaAudit.
    options.ListenToRabbitQueue(AuditQueues.Events);

    // Outbound: commands destined to the Authentication queue.
    options.PublishMessage<CreateAccountCommand>().ToRabbitQueue(RabbitQueues.AuthCommands);
    options.PublishMessage<SendVerifyCommand>().ToRabbitQueue(RabbitQueues.AuthCommands);
    options.PublishMessage<SoftDeleteCommand>().ToRabbitQueue(RabbitQueues.AuthCommands);
    options.PublishMessage<UpdateAccountCommand>().ToRabbitQueue(RabbitQueues.AuthCommands);

    // Outbound: commands destined to the Doctor queue.
    options.PublishMessage<CreateDoctorProfileCommand>().ToRabbitQueue(RabbitQueues.DoctorCommands);
    options
        .PublishMessage<UpdateDoctorMediaIdCommand>()
        .ToRabbitQueue(RabbitQueues.DoctorCommands);
    options.PublishMessage<SoftDeleteDoctorCommand>().ToRabbitQueue(RabbitQueues.DoctorCommands);
    options
        .PublishMessage<UpdateDoctorProfileCommand>()
        .ToRabbitQueue(RabbitQueues.DoctorCommands);
    options.PublishMessage<CanManageDoctorQuery>().ToRabbitQueue(RabbitQueues.DoctorQueries);
    options.PublishMessage<AuditEvent>().ToRabbitQueue(AuditQueues.Events);
});

var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<SagaAuditDbContext>();
    await db.Database.MigrateAsync();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseApiDocumentation();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

await app.RunAsync();
