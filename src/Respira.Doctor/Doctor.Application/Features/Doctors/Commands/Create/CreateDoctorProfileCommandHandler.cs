using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Respira.Doctor.Application.Constracts.Data;
using Respira.Doctor.Application.Features.Doctors.Commands.Create.Events;
using Respira.Doctor.Domain.Enums;
using Respira.ServiceDefaults.Contracts.CQRS;
using Wolverine;
using DoctorEntity = Respira.Doctor.Domain.Entities.Doctor;
using ManagerEntity = Respira.Doctor.Domain.Entities.Manager;

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
                || await dbContext.Managers.AnyAsync(
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
                if (command.Role == ProfileRole.Manager)
                {
                    var manager = new ManagerEntity
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

                    profileId = manager.Id;
                    dbContext.Managers.Add(manager);
                }
                else
                {
                    var doctor = new DoctorEntity
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
        }
    }
}
