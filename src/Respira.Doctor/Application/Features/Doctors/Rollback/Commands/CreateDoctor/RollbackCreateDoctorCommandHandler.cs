using Application.Abstracts.Caching;
using Application.Abstracts.Data;
using Application.Features.Doctors.Rollback.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Respira.ServiceDefaults.Constracts.CQRS;
using Wolverine;

namespace Application.Features.Doctors.Rollback.Commands.CreateDoctor
{
    public class RollbackCreateDoctorCommandHandler(
        ILogger<RollbackCreateDoctorCommandHandler> logger,
        IDoctorDbContext dbContext,
        ICacheService cacheService,
        IMessageBus bus
    ) : ICommandHandler<RollbackCreateDoctorCommand>
    {
        public async Task HandleAsync(
            RollbackCreateDoctorCommand command,
            CancellationToken cancellationToken = default
        )
        {
            try
            {
                var doctor = await dbContext.Doctors.FindAsync(command.DoctorId);
                if (doctor is null)
                {
                    logger.LogWarning(
                        "Doctor {DoctorId} not found for create rollback",
                        command.DoctorId
                    );
                    await bus.PublishAsync(
                        new RollbackCreateDoctorSuccess
                        {
                            SagaId = command.SagaId,
                            DoctorId = command.DoctorId,
                        }
                    );
                    return;
                }

                var creator = await dbContext
                    .Doctors.Include(c => c.Subordinates)
                    .FirstOrDefaultAsync(
                        c => c.Subordinates!.Any(s => s.Id == doctor.Id),
                        cancellationToken
                    );
                creator?.Subordinates = [.. creator!.Subordinates!.Where(s => s.Id != doctor.Id)];

                dbContext.Doctors.Remove(doctor);
                await dbContext.SaveChangesAsync();

                await cacheService.RemoveAsync("doctor:info" + doctor.Id);
                if (creator is not null)
                {
                    await cacheService.RemoveAsync("doctor:info" + creator.Id);
                }

                await bus.PublishAsync(
                    new RollbackCreateDoctorSuccess
                    {
                        SagaId = command.SagaId,
                        DoctorId = command.DoctorId,
                    }
                );
            }
            catch (Exception e)
            {
                logger.LogError(e, "Failed to rollback create doctor");
                await bus.PublishAsync(
                    new RollbackCreateDoctorFailure
                    {
                        SagaId = command.SagaId,
                        DoctorId = command.DoctorId,
                    }
                );
            }
        }
    }
}
