using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Respira.Doctor.Application.Constracts.Data;
using Respira.Doctor.Application.Features.Doctors.Commands.SoftDelete.Events;
using Respira.Doctor.Domain.Entities;
using Respira.ServiceDefaults.Contracts.CQRS;
using Wolverine;

namespace Respira.Doctor.Application.Features.Doctors.Commands.SoftDelete
{
    public class SoftDeleteDoctorCommandHandler(
        IDoctorDbContext dbContext,
        IMessageBus bus,
        ILogger<SoftDeleteDoctorCommandHandler> logger
    ) : ICommandHandler<SoftDeleteDoctorCommand>
    {
        public async Task HandleAsync(
            SoftDeleteDoctorCommand command,
            CancellationToken cancellationToken = default
        )
        {
            BaseDoctor? profile = await dbContext.Doctors.FirstOrDefaultAsync(
                x => x.Id == command.DoctorId,
                cancellationToken
            );
            profile ??= await dbContext.Managers.FirstOrDefaultAsync(
                x => x.Id == command.DoctorId,
                cancellationToken
            );

            if (profile is null)
            {
                // Compensation must be idempotent: "already gone" counts as success.
                logger.LogDebug(
                    "{SagaId} Doctor profile {DoctorId} already gone, treating soft delete as completed",
                    command.SagaId,
                    command.DoctorId
                );

                await bus.PublishAsync(
                    new SoftDeleteDoctorCompleted(command.SagaId, command.DoctorId)
                );
                return;
            }

            try
            {
                profile.IsDeleted = true;
                profile.DeletedAt = DateTimeOffset.UtcNow;

                await dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (Exception exception)
            {
                logger.LogError(
                    exception,
                    "{SagaId} Failed to soft delete doctor profile {DoctorId}",
                    command.SagaId,
                    command.DoctorId
                );

                await bus.PublishAsync(
                    new SoftDeleteDoctorFailure(command.SagaId, exception.Message)
                );
                return;
            }

            logger.LogInformation(
                "{SagaId} Soft deleted doctor profile {DoctorId}",
                command.SagaId,
                command.DoctorId
            );

            await bus.PublishAsync(
                new SoftDeleteDoctorCompleted(command.SagaId, command.DoctorId)
            );
        }
    }
}
