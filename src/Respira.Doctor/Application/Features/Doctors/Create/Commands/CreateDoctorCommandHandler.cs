using Application.Abstracts.Caching;
using Application.Abstracts.Data;
using Application.Features.Doctors.Create.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Respira.ServiceDefaults.Contracts.CQRS;
using Wolverine;

namespace Application.Features.Doctors.Create.Commands
{
    /// <summary>
    /// Handles doctor profile creation and emits success/failure events to the CreateDoctor saga.
    /// </summary>
    public class CreateDoctorCommandHandler(
        ILogger<CreateDoctorCommand> logger,
        IDoctorDbContext dbContext,
        ICacheService cacheService,
        IMessageBus bus
    ) : ICommandHandler<CreateDoctorCommand>
    {
        /// <summary>
        /// Creates the doctor profile, links it to its creator (if any), refreshes the creator
        /// cache, then publishes a success or failure event back to the saga.
        /// </summary>
        /// <param name="command">Create doctor command</param>
        /// <param name="cancellationToken">Cancellation token</param>
        public async Task HandleAsync(
            CreateDoctorCommand command,
            CancellationToken cancellationToken = default
        )
        {
            try
            {
                var isExistingDoctor = await dbContext.Doctors.AnyAsync(
                    x => x.CitizenIdentificationNumber == command.CitizenIdentificationNumber,
                    cancellationToken
                );
                if (isExistingDoctor)
                {
                    logger.LogWarning(
                        "Doctor with CIN {Cin} already exists",
                        command.CitizenIdentificationNumber
                    );
                    await bus.PublishAsync(
                        new CreateDoctorFailure()
                        {
                            SagaId = command.SagaId,
                            Message = "Doctor with CIN already exists",
                        }
                    );
                    return;
                }

                var doctor = new Domain.Entities.Doctor()
                {
                    Id = command.DoctorId,
                    FirstName = command.FirstName,
                    LastName = command.LastName,
                    Degrees = command.Degrees,
                    AcademicTitle = command.AcademicTitle,
                    Position = command.Position,
                    Gender = command.Gender,
                    CitizenIdentificationNumber = command.CitizenIdentificationNumber,
                    DateOfBirth = command.DateOfBirth?.ToUniversalTime(),
                    Address = command.Address,
                    MediaId = command.MediaId,
                };
                dbContext.Doctors.Add(doctor);

                if (command.DoctorCreatorId is not null)
                {
                    var doctorCreator = await dbContext
                        .Doctors.Include(x => x.Subordinates)
                        .FirstOrDefaultAsync(
                            x => x.Id == command.DoctorCreatorId,
                            cancellationToken
                        );
                    if (doctorCreator is not null)
                    {
                        doctorCreator.Subordinates ??= [];
                        doctorCreator.Subordinates.Add(doctor);
                        await cacheService.SetAsync(
                            "doctor:info" + doctorCreator.Id,
                            doctorCreator,
                            TimeSpan.FromMinutes(15)
                        );
                    }
                }
                await dbContext.SaveChangesAsync();
                await bus.PublishAsync(
                    new CreateDoctorSuccess() { SagaId = command.SagaId, DoctorId = doctor.Id }
                );
            }
            catch (Exception e)
            {
                logger.LogError(e, "Failed to create doctor");
                await bus.PublishAsync(
                    new CreateDoctorFailure()
                    {
                        SagaId = command.SagaId,
                        Message = "Failed to create doctor",
                    }
                );
            }
        }
    }
}
