using Application.Abstracts.Caching;
using Application.Abstracts.Data;
using Application.Features.Doctors.Rollback.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Respira.ServiceDefaults.Constracts.CQRS;
using Wolverine;

namespace Application.Features.Doctors.Rollback.Commands.DeleteDoctor
{
    public class RollbackDeleteDoctorCommandHandler(
        ILogger<RollbackDeleteDoctorCommandHandler> logger,
        IDoctorDbContext dbContext,
        ICacheService cacheService,
        IMessageBus bus
    ) : ICommandHandler<RollbackDeleteDoctorCommand>
    {
        public async Task HandleAsync(
            RollbackDeleteDoctorCommand command,
            CancellationToken cancellationToken = default
        )
        {
            try
            {
                var doctor = await dbContext.Doctors
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(x => x.Id == command.DoctorId, cancellationToken);
                if (doctor is null)
                {
                    logger.LogWarning(
                        "Doctor {DoctorId} not found for delete rollback",
                        command.DoctorId
                    );
                    await bus.PublishAsync(
                        new RollbackDeleteDoctorFailure
                        {
                            SagaId = command.SagaId,
                            DoctorId = command.DoctorId,
                        }
                    );
                    return;
                }

                doctor.IsDeleted = false;
                doctor.DeletedAt = null;
                doctor.UpdatedAt = DateTimeOffset.UtcNow;

                var creator = await dbContext.Doctors
                    .Include(c => c.Subordinates)
                    .FirstOrDefaultAsync(
                        c => c.Id == command.DoctorCreatorId,
                        cancellationToken
                    );
                if (creator is not null && creator.Subordinates?.Any(s => s.Id == doctor.Id) == false)
                {
                    creator.Subordinates ??= new List<Domain.Entities.Doctor>();
                    creator.Subordinates.Add(doctor);
                    await cacheService.RemoveAsync("doctor:info" + creator.Id);
                }

                dbContext.Doctors.Update(doctor);
                await dbContext.SaveChangesAsync();

                await cacheService.RemoveAsync("doctor:info" + doctor.Id);

                await bus.PublishAsync(
                    new RollbackDeleteDoctorSuccess
                    {
                        SagaId = command.SagaId,
                        DoctorId = command.DoctorId,
                    }
                );
            }
            catch (Exception e)
            {
                logger.LogError(e, "Failed to rollback delete doctor");
                await bus.PublishAsync(
                    new RollbackDeleteDoctorFailure
                    {
                        SagaId = command.SagaId,
                        DoctorId = command.DoctorId,
                    }
                );
            }
        }
    }
}
