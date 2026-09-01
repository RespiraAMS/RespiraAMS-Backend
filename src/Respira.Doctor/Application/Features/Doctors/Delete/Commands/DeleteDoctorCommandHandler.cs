using Application.Abstracts.Caching;
using Application.Abstracts.Data;
using Application.Features.Doctors.Delete.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Respira.ServiceDefaults.Contracts.CQRS;
using Wolverine;

namespace Application.Features.Doctors.Delete.Commands
{
    /// <summary>
    /// Handles soft-deletion of a doctor profile and emits success/failure events to the
    /// DeleteDoctor saga, detaching the doctor from its creator's subordinates and cache.
    /// </summary>
    public class DeleteDoctorCommandHandler(
        ILogger<DeleteDoctorCommand> logger,
        IDoctorDbContext dbContext,
        ICacheService cacheService,
        IMessageBus bus
    ) : ICommandHandler<DeleteDoctorCommand>
    {
        /// <summary>
        /// Soft-deletes the doctor profile, updates the creator's subordinate list and cache,
        /// then publishes a success or failure event back to the saga.
        /// </summary>
        /// <param name="command">Delete doctor command</param>
        /// <param name="cancellationToken">Cancellation token</param>
        public async Task HandleAsync(
            DeleteDoctorCommand command,
            CancellationToken cancellationToken = default
        )
        {
            try
            {
                var doctor = await dbContext.Doctors.FirstOrDefaultAsync(
                    x => x.Id == command.DoctorId,
                    cancellationToken
                );
                if (doctor is null)
                {
                    logger.LogWarning("Doctor {DoctorId} not found for delete", command.DoctorId);
                    await bus.PublishAsync(
                        new DeleteDoctorSuccess
                        {
                            SagaId = command.SagaId,
                            DoctorId = command.DoctorId,
                        }
                    );
                    return;
                }

                if (command.DoctorCreatorId is not null)
                {
                    var creator = await dbContext
                        .Doctors.Include(c => c.Subordinates)
                        .FirstOrDefaultAsync(
                            c => c.Id == command.DoctorCreatorId,
                            cancellationToken
                        );
                    if (
                        creator is not null
                        && creator.Subordinates?.Any(s => s.Id == doctor.Id) == true
                    )
                    {
                        creator.Subordinates =
                        [
                            .. creator.Subordinates.Where(s => s.Id != doctor.Id),
                        ];
                        await cacheService.RemoveAsync("doctor:info" + creator.Id);
                    }
                }

                doctor.IsDeleted = true;
                doctor.DeletedAt = DateTimeOffset.UtcNow;
                doctor.UpdatedAt = DateTimeOffset.UtcNow;
                dbContext.Doctors.Update(doctor);
                await dbContext.SaveChangesAsync();

                await cacheService.RemoveAsync("doctor:info" + doctor.Id);

                await bus.PublishAsync(
                    new DeleteDoctorSuccess { SagaId = command.SagaId, DoctorId = command.DoctorId }
                );
            }
            catch (Exception e)
            {
                logger.LogError(e, "Failed to delete doctor");
                await bus.PublishAsync(
                    new DeleteDoctorFailure
                    {
                        SagaId = command.SagaId,
                        DoctorId = command.DoctorId,
                        Message = "Failed to delete doctor",
                    }
                );
            }
        }
    }
}
