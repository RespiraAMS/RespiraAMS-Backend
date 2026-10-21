using Authentication.Application.Features.Accounts.Commands.SoftDelete;
using Authentication.Application.Features.Accounts.Commands.SoftDelete.Events;
using Microsoft.Extensions.Logging;
using Respira.Doctor.Application.Features.Doctors.Commands.SoftDelete;
using Respira.Doctor.Application.Features.Doctors.Commands.SoftDelete.Events;
using Respira.Saga.Application.Constracts.Media;
using Respira.Saga.Domain.Enums;
using Respira.ServiceDefaults.Contracts.Results;
using Wolverine;

namespace Respira.Saga.Application.Features.Delete.Commands
{
    /// <summary>
    /// Orchestrates a profile deletion: soft delete the profile → soft delete the account
    /// → delete the avatar's media asset.
    /// </summary>
    /// <remarks>
    /// Like every saga here it has a public parameterless constructor, so dependencies are
    /// handler method parameters, the id member is <see cref="SagaId"/> and the start
    /// method is <c>StartAsync</c>.
    ///
    /// This flow does not compensate (agreed failure policy): a failed step moves the saga
    /// straight to <see cref="DeleteStep.Failed"/> with a reason and the client re-sends
    /// the whole flow, which is safe because every step treats "already gone" as success.
    ///
    /// The soft delete events are shared with <see cref="Features.Create.Commands.CreateSaga"/>,
    /// so with <c>MultipleHandlerBehavior.Separated</c> both sagas receive every one of
    /// them; the static <c>NotFound</c> methods below absorb the foreign ids.
    /// </remarks>
    public class DeleteSaga : Wolverine.Saga
    {
        // ---------------------------------------------------------------- identity
        /// <summary>Identifier shared with every command and event of this flow.</summary>
        public Guid SagaId { get; set; }

        // -------------------------------------------------------- deletion targets
        public Guid AccountId { get; set; }

        public Guid DoctorId { get; set; }

        public Guid MediaId { get; set; }

        // ------------------------------------------------------- step results
        /// <summary>Where the flow currently is.</summary>
        public DeleteStep Step { get; set; }

        /// <summary>Why the flow failed, when it did.</summary>
        public string? FailureReason { get; set; }

        // =====================================================================
        // Step 1 — soft delete the profile
        // =====================================================================

        /// <summary>Starts a new saga instance and fires the first command.</summary>
        public async Task StartAsync(StartDeleteCommand command, IMessageBus bus)
        {
            SagaId = command.SagaId;
            AccountId = command.AccountId;
            DoctorId = command.DoctorId;
            MediaId = command.MediaId;
            Step = DeleteStep.DeletingProfile;

            await bus.SendAsync(
                new SoftDeleteDoctorCommand { SagaId = SagaId, DoctorId = DoctorId }
            );
        }

        // =====================================================================
        // Step 2 — soft delete the account the profile belonged to
        // =====================================================================

        public async Task HandleAsync(SoftDeleteDoctorCompleted message, IMessageBus bus)
        {
            Step = DeleteStep.DeletingAccount;

            await bus.SendAsync(new SoftDeleteCommand { SagaId = SagaId, AccountId = AccountId });
        }

        public Task HandleAsync(SoftDeleteDoctorFailure message)
        {
            // No compensation by agreement: the profile survives and the flow is reported
            // as failed, so the client can re-send it.
            Complete(DeleteStep.Failed, message.Reason ?? "The profile could not be deleted");
            return Task.CompletedTask;
        }

        // =====================================================================
        // Step 3 — delete the avatar's media asset
        // =====================================================================

        public async Task HandleAsync(
            SoftDeleteCompleted message,
            IMediaClient mediaClient,
            ILogger<DeleteSaga> logger,
            CancellationToken cancellationToken
        )
        {
            Step = DeleteStep.DeletingMedia;

            if (MediaId != Guid.Empty)
            {
                Result<Guid> deletion;

                try
                {
                    deletion = await mediaClient.DeleteAssetAsync(MediaId, cancellationToken);
                }
                catch (Exception exception)
                {
                    logger.LogError(
                        exception,
                        "{SagaId} Deleting the media asset {MediaId} failed",
                        SagaId,
                        MediaId
                    );
                    deletion = Result<Guid>.Failure(
                        new ApplicationError(ApplicationStatus.ThirdPartyServiceFailure, exception.Message)
                    );
                }

                if (deletion.IsFailure())
                {
                    Complete(
                        DeleteStep.Failed,
                        deletion.Error?.Description ?? "The media asset could not be deleted"
                    );
                    return;
                }
            }

            Complete(DeleteStep.Completed);
        }

        public Task HandleAsync(SoftDeleteFailure message)
        {
            Complete(
                DeleteStep.Failed,
                message.Reason ?? "The account could not be deleted"
            );
            return Task.CompletedTask;
        }

        // =====================================================================
        // Foreign saga ids (the soft delete events are shared with CreateSaga)
        // =====================================================================

        /// <summary>
        /// Absorbs a soft delete event that belongs to the profile creation compensation instead
        /// of letting Wolverine throw <c>UnknownSagaException</c>. Static, because there is
        /// no instance to run on.
        /// </summary>
        public static Task NotFound(SoftDeleteDoctorCompleted message)
        {
            return Task.CompletedTask;
        }

        public static Task NotFound(SoftDeleteDoctorFailure message)
        {
            return Task.CompletedTask;
        }

        public static Task NotFound(SoftDeleteCompleted message)
        {
            return Task.CompletedTask;
        }

        public static Task NotFound(SoftDeleteFailure message)
        {
            return Task.CompletedTask;
        }

        // =====================================================================
        // Internals
        // =====================================================================

        /// <summary>Moves the saga to a terminal state and lets Wolverine delete it.</summary>
        private void Complete(DeleteStep step, string? reason = null)
        {
            Step = step;
            if (reason is not null)
            {
                FailureReason = reason;
            }

            MarkCompleted();
        }
    }
}
