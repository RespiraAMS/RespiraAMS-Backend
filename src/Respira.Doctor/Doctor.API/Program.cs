using FluentValidation;
using Respira.Doctor.API.Extensions;
using Respira.Doctor.Application.Constracts.Messaging;
using Respira.Doctor.Application.Features.Doctors.Commands.Create;
using Respira.Doctor.Application.Features.Doctors.Commands.Create.Events;
using Respira.Doctor.Application.Features.Doctors.Commands.SoftDelete.Events;
using Respira.Doctor.Application.Features.Doctors.Commands.Update.Events;
using Respira.Doctor.Application.Features.Doctors.Queries.SearchDoctor;
using Respira.Doctor.Infrastructure.Extensions;
using Respira.Doctor.Infrastructure.Persistence;
using Respira.ServiceDefaults.Contracts.Messages.Accounts;
using Respira.ServiceDefaults.Contracts.Messages.Audit;
using Respira.ServiceDefaults.Contracts.Messages.Media;
using Respira.ServiceDefaults.Extensions;
using Wolverine;
using Wolverine.FluentValidation;
using Wolverine.Postgresql;
using Wolverine.RabbitMQ;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddApiVersion();

builder.AddNpgsqlDbContext<DoctorDbContext>("doctordb");
builder.Services.AddDoctorInfrastructure();
builder.Services.AddValidatorsFromAssemblyContaining<SearchDoctorQuery>();

// Discover command/query handlers in the Doctor.Application assembly
builder.Host.UseWolverine(options =>
{
    options.UseRuntimeCompilation();
    options.Discovery.IncludeAssembly(typeof(CreateDoctorProfileCommand).Assembly);
    options.UseFluentValidation(RegistrationBehavior.ExplicitRegistration);
    var mqconn = builder.Configuration.GetConnectionString("rabbitmq") ?? throw new InvalidOperationException("Connection string 'rabbitmq' is not configured");
    options.UseRabbitMq(mqconn).AutoProvision();
    var conn = builder.Configuration.GetConnectionString("doctordb")
        ?? throw new InvalidOperationException("Connection string 'doctordb' is not configured");
    options.PersistMessagesWithPostgresql(conn);
    options.ListenToRabbitQueue(RabbitQueues.DoctorCommands);
    options.ListenToRabbitQueue(RabbitQueues.DoctorQueries);
    options.PublishMessage<DoctorProfileCreated>().ToRabbitQueue(RabbitQueues.SagaEvents);
    options.PublishMessage<DoctorProfileFailure>().ToRabbitQueue(RabbitQueues.SagaEvents);
    options.PublishMessage<DoctorMediaUpdated>().ToRabbitQueue(RabbitQueues.SagaEvents);
    options.PublishMessage<DoctorMediaUpdateFailure>().ToRabbitQueue(RabbitQueues.SagaEvents);
    options.PublishMessage<DoctorProfileUpdated>().ToRabbitQueue(RabbitQueues.SagaEvents);
    options
        .PublishMessage<DoctorProfileUpdateFailure>()
        .ToRabbitQueue(RabbitQueues.SagaEvents);
    options.PublishMessage<SoftDeleteDoctorCompleted>().ToRabbitQueue(RabbitQueues.SagaEvents);
    options.PublishMessage<SoftDeleteDoctorFailure>().ToRabbitQueue(RabbitQueues.SagaEvents);

    // Outbound: lookups used to enrich doctor search/list results with account and
    // media data owned by the other microservices.
    options.PublishMessage<GetAccountsByIdsQuery>().ToRabbitQueue(RabbitQueues.AuthQueries);
    options.PublishMessage<GetMediaAssetsByIdsQuery>().ToRabbitQueue(RabbitQueues.MediaQueries);
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
