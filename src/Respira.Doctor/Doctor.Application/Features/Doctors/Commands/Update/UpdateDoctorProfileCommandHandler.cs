using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Respira.Doctor.Application.Constracts.Data;
using Respira.Doctor.Application.Features.Doctors.Commands.Update.Events;
using Respira.Doctor.Domain.Entities;
using Respira.ServiceDefaults.Contracts.CQRS;
using Wolverine;

namespace Respira.Doctor.Application.Features.Doctors.Commands.Update
{
    public class UpdateDoctorProfileCommandHandler(
        IDoctorDbContext dbContext,
        IMessageBus bus,
        ILogger<UpdateDoctorProfileCommandHandler> logger
    ) : ICommandHandler<UpdateDoctorProfileCommand>
    {
        public async Task HandleAsync(
            UpdateDoctorProfileCommand command,
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
                    "{SagaId} Doctor profile {DoctorId} not found for update",
                    command.SagaId,
                    command.DoctorId
                );

                await bus.PublishAsync(
                    new DoctorProfileUpdateFailure(command.SagaId, "Profile not found")
                );
                return;
            }

            // The avatar that is about to be replaced; the saga deletes it once the
            // update is acknowledged. On a retry both ids are already equal, so the
            // cleanup is skipped and the flow stays idempotent.
            var oldMediaId = profile.MediaId ?? Guid.Empty;

            try
            {
                profile.FirstName = command.FirstName;
                profile.LastName = command.LastName;
                profile.Degrees = command.Degrees;
                profile.AcademicTitle = command.AcademicTitle;
                profile.Position = command.Position;
                profile.Gender = command.Gender;
                profile.CitizenIdentificationNumber = command.CitizenIdentificationNumber;
                profile.DateOfBirth = command.DateOfBirth;
                profile.Address = command.Address;

                if (command.NewMediaId != Guid.Empty)
                {
                    profile.MediaId = command.NewMediaId;
                }

                profile.UpdatedAt = DateTimeOffset.UtcNow;

                await dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (Exception exception)
            {
                logger.LogError(
                    exception,
                    "{SagaId} Failed to update doctor profile {DoctorId}",
                    command.SagaId,
                    command.DoctorId
                );

                await bus.PublishAsync(
                    new DoctorProfileUpdateFailure(command.SagaId, exception.Message)
                );
                return;
            }

            logger.LogInformation(
                "{SagaId} Updated doctor profile {DoctorId} (media {MediaId}, previous {OldMediaId})",
                command.SagaId,
                command.DoctorId,
                profile.MediaId,
                oldMediaId
            );

            await bus.PublishAsync(
                new DoctorProfileUpdated(
                    command.SagaId,
                    command.DoctorId,
                    profile.MediaId ?? Guid.Empty,
                    oldMediaId
                )
            );
        }
    }
}
