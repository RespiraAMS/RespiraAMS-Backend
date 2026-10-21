using Authentication.Application.Features.Accounts.Commands.Update;
using Authentication.Application.Features.Accounts.Commands.Update.Events;
using Microsoft.Extensions.Logging;
using Respira.Doctor.Application.Features.Doctors.Commands.Update;
using Respira.Doctor.Application.Features.Doctors.Commands.Update.Events;
using Respira.Doctor.Domain.Enums;
using Respira.Saga.Application.Constracts.Media;
using Respira.Saga.Domain.Enums;
using Respira.ServiceDefaults.Contracts.Results;
using Wolverine;

namespace Respira.Saga.Application.Features.Update.Commands
{
    /// <summary>
    /// Orchestrates a profile update: update the account → upload the new avatar →
    /// update the profile (and point it at the new avatar) → delete the avatar that was
    /// replaced.
    /// </summary>
    /// <remarks>
    /// Like every saga here it has a public parameterless constructor, so dependencies are
    /// handler method parameters, the id member is <see cref="SagaId"/> and the start
    /// method is <c>StartAsync</c>.
    ///
    /// This flow does not compensate (agreed failure policy): any step that fails moves
    /// the saga straight to <see cref="UpdateStep.Failed"/> with a reason, and the client
    /// re-sends the whole flow, which is safe because every step is idempotent.
    /// </remarks>
    public class UpdateSaga : Wolverine.Saga
    {
        // ---------------------------------------------------------------- identity
        /// <summary>Identifier shared with every command and event of this flow.</summary>
        public Guid SagaId { get; set; }

        // ---------------------------------------------------------- update targets
        public Guid AccountId { get; set; }

        public Guid DoctorId { get; set; }

        // -------------------------------------------------------- profile input
        public string Email { get; set; } = string.Empty;

        public string Phone { get; set; } = string.Empty;

        public string FirstName { get; set; } = string.Empty;

        public string LastName { get; set; } = string.Empty;

        public List<DegreeType> Degrees { get; set; } = [];

        public AcademicTitleEnum AcademicTitle { get; set; }

        public PositionType Position { get; set; }

        public GenderType Gender { get; set; }

        public string CitizenIdentificationNumber { get; set; } = string.Empty;

        public DateTimeOffset? DateOfBirth { get; set; }

        public string Address { get; set; } = string.Empty;

        /// <summary>Temp avatar written by the endpoint, consumed by the media step.</summary>
        public string? TempAvatarPath { get; set; }

        // ------------------------------------------------------- step results
        /// <summary>Identifier of the newly uploaded avatar.</summary>
        public Guid MediaId { get; set; }

        /// <summary>Where the flow currently is.</summary>
        public UpdateStep Step { get; set; }

        /// <summary>Why the flow failed, when it did.</summary>
        public string? FailureReason { get; set; }

        // =====================================================================
        // Step 1 — update the account
        // =====================================================================

        /// <summary>Starts a new saga instance and fires the first command.</summary>
        public async Task StartAsync(StartUpdateCommand command, IMessageBus bus)
        {
            SagaId = command.SagaId;
            AccountId = command.AccountId;
            DoctorId = command.DoctorId;
            Email = command.Email.Trim().ToLowerInvariant();
            Phone = command.Phone;
            FirstName = command.FirstName;
            LastName = command.LastName;
            Degrees = command.Degrees;
            AcademicTitle = command.AcademicTitle;
            Position = command.Position;
            Gender = command.Gender;
            CitizenIdentificationNumber = command.CitizenIdentificationNumber;
            DateOfBirth = command.DateOfBirth;
            Address = command.Address;
            TempAvatarPath = command.TempAvatarPath;
            Step = UpdateStep.UpdatingAccount;

            // The plaintext password only lives inside this outgoing command; it is never
            // assigned to saga state, so it is never persisted in the saga table. Role and
            // status stay untouched: this flow only changes contact data and the profile.
            await bus.SendAsync(
                new UpdateAccountCommand
                {
                    SagaId = SagaId,
                    AccountId = AccountId,
                    Email = Email,
                    Phone = Phone,
                    Password = command.Password,
                }
            );
        }

        // =====================================================================
        // Step 2 — upload the new avatar, then send the profile update
        // =====================================================================

        public async Task HandleAsync(
            UpdateAccountCompleted message,
            IMessageBus bus,
            IMediaClient mediaClient,
            ILogger<UpdateSaga> logger,
            CancellationToken cancellationToken
        )
        {
            if (string.IsNullOrWhiteSpace(TempAvatarPath))
            {
                await SendProfileUpdateAsync(bus, Guid.Empty);
                return;
            }

            Step = UpdateStep.UploadingAvatar;

            Result<Guid> upload;

            try
            {
                if (string.IsNullOrWhiteSpace(TempAvatarPath) || !File.Exists(TempAvatarPath))
                {
                    upload = Result<Guid>.Failure(
                        new ApplicationError(ApplicationStatus.BadRequest, "The avatar file is missing")
                    );
                }
                else
                {
                    await using var avatar = File.OpenRead(TempAvatarPath);
                    upload = await mediaClient.UploadAvatarAsync(
                        avatar,
                        Path.GetFileName(TempAvatarPath),
                        $"doctors/{DoctorId}/avatar",
                        cancellationToken
                    );
                }
            }
            catch (Exception exception)
            {
                // Never throw out of a saga handler: a blind retry would upload the file twice.
                logger.LogError(exception, "{SagaId} Uploading the avatar failed", SagaId);
                upload = Result<Guid>.Failure(
                    new ApplicationError(ApplicationStatus.ThirdPartyServiceFailure, exception.Message)
                );
            }
            finally
            {
                TryDeleteTempAvatar();
            }

            if (upload.IsFailure() || upload.Data == Guid.Empty)
            {
                Complete(
                    UpdateStep.Failed,
                    upload.Error?.Description ?? "The avatar could not be uploaded"
                );
                return;
            }

            MediaId = upload.Data;
            await SendProfileUpdateAsync(bus, MediaId);
        }

        public Task HandleAsync(UpdateAccountFailure message)
        {
            // First step, nothing on the profile changed yet.
            Complete(UpdateStep.Failed, message.Reason);
            return Task.CompletedTask;
        }

        // =====================================================================
        // Step 3 — drop the avatar that was just replaced
        // =====================================================================

        public async Task HandleAsync(
            DoctorProfileUpdated message,
            IMediaClient mediaClient,
            ILogger<UpdateSaga> logger,
            CancellationToken cancellationToken
        )
        {
            MediaId = message.MediaId;
            Step = UpdateStep.DeletingOldMedia;

            // A retry reports the media it had already linked as the old one, so this
            // cleanup naturally does nothing the second time around.
            if (message.OldMediaId != Guid.Empty && message.OldMediaId != message.MediaId)
            {
                Result<Guid> deletion;

                try
                {
                    deletion = await mediaClient.DeleteAssetAsync(
                        message.OldMediaId,
                        cancellationToken
                    );
                }
                catch (Exception exception)
                {
                    logger.LogError(
                        exception,
                        "{SagaId} Deleting the replaced avatar {MediaId} failed",
                        SagaId,
                        message.OldMediaId
                    );
                    deletion = Result<Guid>.Failure(
                        new ApplicationError(ApplicationStatus.ThirdPartyServiceFailure, exception.Message)
                    );
                }

                if (deletion.IsFailure())
                {
                    Complete(
                        UpdateStep.Failed,
                        deletion.Error?.Description
                            ?? "The replaced avatar could not be deleted"
                    );
                    return;
                }
            }

            Complete(UpdateStep.Completed);
        }

        public Task HandleAsync(DoctorProfileUpdateFailure message)
        {
            Complete(UpdateStep.Failed, message.Reason);
            return Task.CompletedTask;
        }

        // =====================================================================
        // Internals
        // =====================================================================

        /// <summary>Moves the saga to a terminal state and lets Wolverine delete it.</summary>
        private void Complete(UpdateStep step, string? reason = null)
        {
            Step = step;
            if (reason is not null)
            {
                FailureReason = reason;
            }

            TryDeleteTempAvatar();
            MarkCompleted();
        }

        private void TryDeleteTempAvatar()
        {
            if (string.IsNullOrWhiteSpace(TempAvatarPath))
            {
                return;
            }

            try
            {
                if (File.Exists(TempAvatarPath))
                {
                    File.Delete(TempAvatarPath);
                }
            }
            catch (IOException)
            {
                // The file is a leftover of a failed run; the operating system will
                // eventually clean the temp folder anyway.
            }
            catch (UnauthorizedAccessException)
            {
                // Same as above: never fail the flow over temp file hygiene.
            }

            TempAvatarPath = null;
        }

        private async Task SendProfileUpdateAsync(IMessageBus bus, Guid newMediaId)
        {
            Step = UpdateStep.UpdatingProfile;
            await bus.SendAsync(
                new UpdateDoctorProfileCommand
                {
                    SagaId = SagaId,
                    DoctorId = DoctorId,
                    FirstName = FirstName,
                    LastName = LastName,
                    Degrees = Degrees,
                    AcademicTitle = AcademicTitle,
                    Position = Position,
                    Gender = Gender,
                    CitizenIdentificationNumber = CitizenIdentificationNumber,
                    DateOfBirth = DateOfBirth,
                    Address = Address,
                    NewMediaId = newMediaId,
                }
            );
        }
    }
}
