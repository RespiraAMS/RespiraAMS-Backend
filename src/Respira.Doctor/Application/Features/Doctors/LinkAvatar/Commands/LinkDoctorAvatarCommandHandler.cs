using Application.Abstracts.Caching;
using Application.Abstracts.Data;
using Application.Features.Doctors.LinkAvatar.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Respira.ServiceDefaults.Constracts.CQRS;
using Wolverine;

namespace Application.Features.Doctors.LinkAvatar.Commands
{
    /// <summary>
    /// Sets a doctor's <c>MediaId</c> to the supplied avatar and publishes the result event
    /// back to the saga that initiated the link. Handles both the create-flow command
    /// (<see cref="LinkDoctorAvatarCommand"/>) and the update-flow command
    /// (<see cref="UpdateDoctorLinkAvatarCommand"/>) so each saga gets its own event pair.
    /// </summary>
    public class LinkDoctorAvatarCommandHandler(
        ILogger<LinkDoctorAvatarCommandHandler> logger,
        IDoctorDbContext dbContext,
        ICacheService cacheService,
        IMessageBus bus
    ) : ICommandHandler<LinkDoctorAvatarCommand>, ICommandHandler<UpdateDoctorLinkAvatarCommand>
    {
        private const string CacheKeyPrefix = "doctor:info";

        /// <summary>
        /// Links the avatar media to the doctor profile in the CreateDoctor saga flow and
        /// publishes the create-flow success/failure event pair.
        /// </summary>
        /// <param name="command">Link avatar command</param>
        /// <param name="cancellationToken">Cancellation token</param>
        public Task HandleAsync(
            LinkDoctorAvatarCommand command,
            CancellationToken cancellationToken = default
        ) =>
            LinkAsync(
                command.SagaId,
                command.DoctorId,
                command.MediaId, // published events
                success: (sagaId, doctorId, mediaId) =>
                    new LinkDoctorAvatarSuccessEvent
                    {
                        SagaId = sagaId,
                        DoctorId = doctorId,
                        MediaId = mediaId,
                    },
                failure: (sagaId, doctorId) =>
                    new LinkDoctorAvatarFailureEvent { SagaId = sagaId, DoctorId = doctorId },
                cancellationToken
            );

        /// <summary>
        /// Links the avatar media to the doctor profile in the UpdateDoctor saga flow and
        /// publishes the update-flow success/failure event pair.
        /// </summary>
        /// <param name="command">Update link avatar command</param>
        /// <param name="cancellationToken">Cancellation token</param>
        public Task HandleAsync(
            UpdateDoctorLinkAvatarCommand command,
            CancellationToken cancellationToken = default
        ) =>
            LinkAsync(
                command.SagaId,
                command.DoctorId,
                command.MediaId,
                success: (sagaId, doctorId, mediaId) =>
                    new UpdateDoctorLinkAvatarSuccessEvent
                    {
                        SagaId = sagaId,
                        DoctorId = doctorId,
                        MediaId = mediaId,
                    },
                failure: (sagaId, doctorId) =>
                    new UpdateDoctorLinkAvatarFailureEvent { SagaId = sagaId, DoctorId = doctorId },
                cancellationToken
            );

        private async Task LinkAsync(
            Guid sagaId,
            Guid doctorId,
            Guid mediaId,
            Func<Guid, Guid, Guid?, object> success,
            Func<Guid, Guid, object> failure,
            CancellationToken cancellationToken
        )
        {
            try
            {
                var doctor = await dbContext.Doctors.FirstOrDefaultAsync(
                    x => x.Id == doctorId,
                    cancellationToken
                );
                if (doctor is null)
                {
                    logger.LogWarning("Doctor {DoctorId} not found for avatar link", doctorId);
                    await bus.PublishAsync(failure(sagaId, doctorId));
                    return;
                }

                doctor.MediaId = mediaId;

                await cacheService.RemoveAsync(CacheKeyPrefix + doctor.Id);
                await dbContext.SaveChangesAsync();

                await bus.PublishAsync(success(sagaId, doctorId, doctor.MediaId));
            }
            catch (Exception e)
            {
                logger.LogError(e, "Failed to link avatar to doctor");
                await bus.PublishAsync(failure(sagaId, doctorId));
            }
        }
    }
}
