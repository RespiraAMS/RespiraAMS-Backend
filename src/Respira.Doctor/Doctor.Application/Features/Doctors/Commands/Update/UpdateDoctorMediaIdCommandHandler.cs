using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Respira.Doctor.Application.Constracts.Data;
using Respira.Doctor.Application.Features.Doctors.Commands.Update.Events;
using Respira.Doctor.Domain.Entities;
using Respira.ServiceDefaults.Contracts.CQRS;
using Wolverine;

namespace Respira.Doctor.Application.Features.Doctors.Commands.Update
{
    public class UpdateDoctorMediaIdCommandHandler(
        IDoctorDbContext dbContext,
        IMessageBus bus,
        ILogger<UpdateDoctorMediaIdCommandHandler> logger
    ) : ICommandHandler<UpdateDoctorMediaIdCommand>
    {
        public async Task HandleAsync(
            UpdateDoctorMediaIdCommand command,
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
                logger.LogDebug(
                    "{SagaId} Doctor profile {DoctorId} not found for media update",
                    command.SagaId,
                    command.DoctorId
                );

                await bus.PublishAsync(
                    new DoctorMediaUpdateFailure(command.SagaId, "Profile not found")
                );
                return;
            }

            try
            {
                profile.MediaId = command.MediaId;
                profile.UpdatedAt = DateTimeOffset.UtcNow;

                await dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (Exception exception)
            {
                logger.LogError(
                    exception,
                    "{SagaId} Failed to update media for doctor profile {DoctorId}",
                    command.SagaId,
                    command.DoctorId
                );

                await bus.PublishAsync(
                    new DoctorMediaUpdateFailure(command.SagaId, exception.Message)
                );
                return;
            }

            logger.LogInformation(
                "{SagaId} Updated media {MediaId} for doctor profile {DoctorId}",
                command.SagaId,
                command.MediaId,
                command.DoctorId
            );

            await bus.PublishAsync(
                new DoctorMediaUpdated(command.SagaId, command.DoctorId, command.MediaId)
            );
        }
    }
}
