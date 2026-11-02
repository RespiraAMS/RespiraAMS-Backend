using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Respira.Doctor.Application.Contracts.Data;
using Respira.Doctor.Application.Features.Doctors.Commands.Create.Events;
using Respira.Doctor.Domain.Enums;
using Respira.ServiceDefaults.Contracts.CQRS;
using Respira.ServiceDefaults.Contracts.Messages.Audit;
using Wolverine;
using DoctorEntity = Respira.Doctor.Domain.Entities.Doctor;
using HeadDepartmentDoctorEntity = Respira.Doctor.Domain.Entities.HeadDepartmentDoctor;

namespace Respira.Doctor.Application.Features.Doctors.Commands.Create
{
    public class CreateDoctorProfileCommandHandler(
        IDoctorDbContext dbContext,
        IMessageBus bus,
        ILogger<CreateDoctorProfileCommandHandler> logger
    ) : ICommandHandler<CreateDoctorProfileCommand>
    {
        public async Task HandleAsync(
            CreateDoctorProfileCommand command,
            CancellationToken cancellationToken = default
        )
        {
            var profileExists =
                await dbContext.Doctors.AnyAsync(
                    x => x.AccountId == command.AccountId,
                    cancellationToken
                )
                || await dbContext.HeadDepartmentDoctors.AnyAsync(
                    x => x.AccountId == command.AccountId,
                    cancellationToken
                );

            if (profileExists)
            {
                logger.LogDebug(
                    "{SagaId} Doctor profile already exists for account {AccountId}",
                    command.SagaId,
                    command.AccountId
                );

                await bus.PublishAsync(
                    new DoctorProfileFailure(command.SagaId, "Profile already exists")
                );
                return;
            }

            Guid profileId;

            try
            {
                if (command.Role == ProfileRole.HeadDepartmentDoctor)
                {
                    var headDepartmentDoctor = new HeadDepartmentDoctorEntity
                    {
                        AccountId = command.AccountId,
                        FirstName = command.FirstName,
                        LastName = command.LastName,
                        Degrees = command.Degrees,
                        AcademicTitle = command.AcademicTitle,
                        Position = command.Position,
                        Gender = command.Gender,
                        CitizenIdentificationNumber = command.CitizenIdentificationNumber,
                        DateOfBirth = command.DateOfBirth,
                        Address = command.Address,
                    };

                    profileId = headDepartmentDoctor.Id;
                    dbContext.HeadDepartmentDoctors.Add(headDepartmentDoctor);
                }
                else
                {
                    Guid? headDepartmentDoctorId = null;
                    if (command.HeadDepartmentDoctorAccountId != Guid.Empty)
                    {
                        headDepartmentDoctorId = await dbContext.HeadDepartmentDoctors
                            .Where(item => item.AccountId == command.HeadDepartmentDoctorAccountId)
                            .Select(item => (Guid?)item.Id)
                            .FirstOrDefaultAsync(cancellationToken);

                        if (!headDepartmentDoctorId.HasValue)
                        {
                            await bus.PublishAsync(
                                new DoctorProfileFailure(
                                    command.SagaId,
                                    "The creating head of department doctor profile was not found"
                                )
                            );
                            return;
                        }
                    }

                    var doctor = new DoctorEntity
                    {
                        AccountId = command.AccountId,
                        HeadDepartmentDoctorId = headDepartmentDoctorId,
                        FirstName = command.FirstName,
                        LastName = command.LastName,
                        Degrees = command.Degrees,
                        AcademicTitle = command.AcademicTitle,
                        Position = command.Position,
                        Gender = command.Gender,
                        CitizenIdentificationNumber = command.CitizenIdentificationNumber,
                        DateOfBirth = command.DateOfBirth,
                        Address = command.Address,
                    };

                    profileId = doctor.Id;
                    dbContext.Doctors.Add(doctor);
                }

                await dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (Exception exception)
            {
                logger.LogError(
                    exception,
                    "{SagaId} Failed to create doctor profile for account {AccountId}",
                    command.SagaId,
                    command.AccountId
                );

                await bus.PublishAsync(
                    new DoctorProfileFailure(command.SagaId, exception.Message)
                );
                return;
            }

            logger.LogInformation(
                "{SagaId} Created doctor profile {DoctorId} for account {AccountId}",
                command.SagaId,
                profileId,
                command.AccountId
            );

            await bus.PublishAsync(
                new DoctorProfileCreated(command.SagaId, profileId, command.AccountId, command.Role)
            );
            await bus.PublishAsync(
                new AuditEvent
                {
                    ActorAccountId = command.CreatorAccountId == Guid.Empty ? null : command.CreatorAccountId,
                    ActorRole = string.IsNullOrWhiteSpace(command.CreatorRole) ? "System" : command.CreatorRole,
                    Action = "Doctor.ProfileCreated",
                    EntityType = command.Role.ToString(),
                    EntityId = profileId,
                    CorrelationId = command.SagaId,
                    DetailsJson = System.Text.Json.JsonSerializer.Serialize(new { command.AccountId }),
                }
            );
        }
    }
}
