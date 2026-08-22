using Application.Abstracts.Caching;
using Application.Abstracts.Data;
using Application.Features.Doctors.Rollback.Events;
using Microsoft.Extensions.Logging;
using Respira.ServiceDefaults.Constracts.CQRS;
using Wolverine;

namespace Application.Features.Doctors.Rollback.Commands.UpdateDoctor
{
    public class RollbackUpdateDoctorCommandHandler(
        ILogger<RollbackUpdateDoctorCommandHandler> logger,
        IDoctorDbContext dbContext,
        ICacheService cacheService,
        IMessageBus bus
    ) : ICommandHandler<RollbackUpdateDoctorCommand>
    {
        public async Task HandleAsync(
            RollbackUpdateDoctorCommand command,
            CancellationToken cancellationToken = default
        )
        {
            try
            {
                var doctor = await dbContext.Doctors.FindAsync(command.DoctorId);
                if (doctor is null)
                {
                    logger.LogWarning($"Doctor {command.DoctorId} not found for rollback");
                    await bus.PublishAsync(
                        new RollbackUpdateDoctorFailure
                        {
                            SagaId = command.SagaId,
                            DoctorId = command.DoctorId,
                        }
                    );
                    return;
                }
                doctor.FirstName = command.FirstName;
                doctor.LastName = command.LastName;
                doctor.Degrees = command.Degrees;
                doctor.AcademicTitle = command.AcademicTitle;
                doctor.Position = command.Position;
                doctor.Gender = command.Gender;
                doctor.CitizenIdentificationNumber = command.CitizenIdentificationNumber;
                doctor.DateOfBirth = command.DateOfBirth;
                doctor.Address = command.Address;
                doctor.MediaId = command.MediaId;

                if (command.DoctorCreatorId is not null)
                {
                    var doctorCreator = await dbContext.Doctors.FindAsync(command.DoctorCreatorId);
                    if (
                        doctorCreator?.Subordinates is not null
                        && doctorCreator.Subordinates.Any(s => s.Id == doctor.Id)
                    )
                    {
                        doctorCreator.Subordinates =
                        [
                            .. doctorCreator.Subordinates.Where(s => s.Id != doctor.Id),
                        ];
                    }
                }

                dbContext.Doctors.Update(doctor);
                await dbContext.SaveChangesAsync();
                await cacheService.RemoveAsync("doctor:info" + doctor.Id);
                await cacheService.SetAsync(
                    "doctor:info" + doctor.Id,
                    doctor,
                    TimeSpan.FromMinutes(15)
                );
                await bus.PublishAsync(
                    new RollbackUpdateDoctorSuccess
                    {
                        SagaId = command.SagaId,
                        DoctorId = command.DoctorId,
                    }
                );
            }
            catch (Exception e)
            {
                logger.LogError(e, "Failed to rollback update doctor");
                await bus.PublishAsync(
                    new RollbackUpdateDoctorFailure
                    {
                        SagaId = command.SagaId,
                        DoctorId = command.DoctorId,
                    }
                );
            }
        }
    }
}
