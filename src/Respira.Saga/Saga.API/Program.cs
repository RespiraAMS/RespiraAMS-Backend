using Asp.Versioning;
using Respira.Saga.Application.Constracts.Messages.Authentication;
using Respira.Saga.Application.Constracts.Messages.Authentication.Create;
using Respira.Saga.Application.Constracts.Messages.Authentication.SoftDelete;
using Respira.Saga.Application.Constracts.Messages.Authentication.Update;
using Respira.Saga.Application.Constracts.Messages.Doctor.Create;
using Respira.Saga.Application.Constracts.Messages.Doctor.SoftDelete;
using Respira.Saga.Application.Constracts.Messages.Doctor.Update;
using Respira.Saga.Application.Constracts.Messaging;
using Respira.Saga.Application.Features.Create.Commands;
using Respira.Saga.Infrastructure.Extensions;
using Respira.ServiceDefaults.Extensions;
using Wolverine;
using Wolverine.Postgresql;
using Wolverine.RabbitMQ;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
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

    // The soft delete events are handled by CreateSaga (compensation) and by
    // DeleteSaga (the delete flow): keep the chains apart so each saga gets its own and
    // can ignore the ids that belong to the other one.
    options.MultipleHandlerBehavior = MultipleHandlerBehavior.Separated;

    // Durable inbox/outbox on PostgreSQL, which also persists the saga state itself.
    options.PersistMessagesWithPostgresql(
        builder.Configuration.GetConnectionString("sagadb")
            ?? throw new InvalidOperationException("Connection string 'sagadb' is not configured")
    );

    options.UseRabbitMqUsingNamedConnection("rabbitmq").AutoProvision();

    // Inbound: completion and failure events raised by Authentication and Doctor.
    options.ListenToRabbitQueue(RabbitQueues.SagaEvents);

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
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseApiDocumentation();
}

app.UseHttpsRedirection();

app.MapControllers();

await app.RunAsync();
