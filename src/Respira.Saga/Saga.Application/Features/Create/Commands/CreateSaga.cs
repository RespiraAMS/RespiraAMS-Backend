using Microsoft.Extensions.Logging;
using Respira.Authentication.Domain.Enums;
using Respira.Doctor.Domain.Enums;
using Respira.Saga.Application.Constracts.Media;
using Respira.Saga.Application.Constracts.Messages.Authentication;
using Respira.Saga.Application.Constracts.Messages.Authentication.Create;
using Respira.Saga.Application.Constracts.Messages.Authentication.Create.Events;
using Respira.Saga.Application.Constracts.Messages.Authentication.Events;
using Respira.Saga.Application.Constracts.Messages.Authentication.SoftDelete;
using Respira.Saga.Application.Constracts.Messages.Authentication.SoftDelete.Events;
using Respira.Saga.Application.Constracts.Messages.Doctor.Create;
using Respira.Saga.Application.Constracts.Messages.Doctor.Create.Events;
using Respira.Saga.Application.Constracts.Messages.Doctor.SoftDelete;
using Respira.Saga.Application.Constracts.Messages.Doctor.SoftDelete.Events;
using Respira.Saga.Application.Constracts.Messages.Doctor.Update;
using Respira.Saga.Application.Constracts.Messages.Doctor.Update.Events;
using Respira.Saga.Domain.Enums;
using Respira.ServiceDefaults.Contracts.Results;
using Wolverine;

namespace Respira.Saga.Application.Features.Create.Commands
{
    /// <summary>
    /// Orchestrates account profile creation: create account → create doctor/manager profile →
    /// upload the avatar → link the media to the profile → send the verification email.
    /// </summary>
    /// <remarks>
    /// Wolverine requires saga types to have a public parameterless constructor, so every
    /// dependency is passed as a handler method parameter instead. The saga id member is
    /// <see cref="SagaId"/>, which matches the <c>SagaId</c> property carried by every wire
    /// message, so each event can locate this instance.
    ///
    /// The handler that starts the flow must be named <c>Start*</c>; every other step uses
    /// <c>Handle*</c>.
    ///
    /// The soft delete events are shared with <see cref="Features.Delete.Commands.DeleteSaga"/>,
    /// so with <c>MultipleHandlerBehavior.Separated</c> every saga receives them. The static
    /// <c>NotFound</c> methods below absorb the ones that belong to another saga's id.
    /// </remarks>
    public class CreateSaga : Wolverine.Saga
    {
        // ---------------------------------------------------------------- identity
        /// <summary>Identifier shared with every command and event of this flow.</summary>
        public Guid SagaId { get; set; }

        // -------------------------------------------------------- profile creation input
        public string Email { get; set; } = string.Empty;

        public string Phone { get; set; } = string.Empty;

        public ProfileRole Role { get; set; }

        public string FirstName { get; set; } = string.Empty;

        public string LastName { get; set; } = string.Empty;

        public List<DegreeType> Degrees { get; set; } = [];

        public AcademicTitle AcademicTitle { get; set; }

        public PositionType Position { get; set; }

        public GenderType Gender { get; set; }

        public string CitizenIdentificationNumber { get; set; } = string.Empty;

        public DateTimeOffset? DateOfBirth { get; set; }

        public string Address { get; set; } = string.Empty;

        public string? TempAvatarPath { get; set; }

        public Guid AccountId { get; set; }

        public Guid DoctorId { get; set; }

        public Guid MediaId { get; set; }

        public CreateStep Step { get; set; }

        public string? FailureReason { get; set; }

        /// <summary>Starts a new saga instance and fires the first command.</summary>
        public async Task StartAsync(StartCreateCommand command, IMessageBus bus)
        {
            SagaId = command.SagaId;
            Email = command.Email.Trim().ToLowerInvariant();
            Phone = command.Phone;
            Role = command.Role;
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
            Step = CreateStep.CreatingAccount;

            // The plaintext password only lives inside this outgoing command; it is never
            // assigned to saga state, so it is never persisted in the saga table.
            await bus.SendAsync(
                new CreateAccountCommand
                {
                    SagaId = SagaId,
                    Email = Email,
                    Password = command.Password,
                    Phone = Phone,
                    Role = Role == ProfileRole.Manager ? RoleType.Manager : RoleType.Doctor,
                    Status = StatusType.Active,
                }
            );
        }

        // =====================================================================
        // Step 2 — create the doctor/manager profile
        // =====================================================================

        public async Task HandleAsync(CreateAccountCompleted message, IMessageBus bus)
        {
            AccountId = message.AccountId;
            Step = CreateStep.CreatingProfile;

            await bus.SendAsync(
                new CreateDoctorProfileCommand
                {
                    SagaId = SagaId,
                    AccountId = AccountId,
                    Role = Role,
                    FirstName = FirstName,
                    LastName = LastName,
                    Degrees = Degrees,
                    AcademicTitle = AcademicTitle,
                    Position = Position,
                    Gender = Gender,
                    CitizenIdentificationNumber = CitizenIdentificationNumber,
                    DateOfBirth = DateOfBirth,
                    Address = Address,
                }
            );
        }

        public Task HandleAsync(CreateAccountFailure message)
        {
            // First step, nothing to roll back yet.
            Complete(CreateStep.Failed, message.Reason);
            return Task.CompletedTask;
        }

        // =====================================================================
        // Step 3 — upload the avatar, then link it to the profile
        // =====================================================================

        public async Task HandleAsync(
            DoctorProfileCreated message,
            IMessageBus bus,
            IMediaClient mediaClient,
            ILogger<CreateSaga> logger,
            CancellationToken cancellationToken
        )
        {
            DoctorId = message.DoctorId;
            Step = CreateStep.CreatingMedia;

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
                await CompensateAsync(
                    bus,
                    upload.Error?.Description ?? "The avatar could not be uploaded"
                );
                return;
            }

            MediaId = upload.Data;
            Step = CreateStep.LinkingMedia;

            await bus.SendAsync(
                new UpdateDoctorMediaIdCommand
                {
                    SagaId = SagaId,
                    DoctorId = DoctorId,
                    MediaId = MediaId,
                }
            );
        }

        public async Task HandleAsync(DoctorProfileFailure message, IMessageBus bus)
        {
            await CompensateAsync(bus, message.Reason ?? "The profile could not be created");
        }

        public async Task HandleAsync(DoctorMediaUpdated message, IMessageBus bus)
        {
            MediaId = message.MediaId;
            Step = CreateStep.SendingVerificationEmail;

            await bus.SendAsync(new SendVerifyCommand { Email = Email, SagaId = SagaId });
        }

        public async Task HandleAsync(DoctorMediaUpdateFailure message, IMessageBus bus)
        {
            await CompensateAsync(
                bus,
                message.Reason ?? "The media could not be linked to the profile"
            );
        }

        public Task HandleAsync(SendVerifyCompleted message)
        {
            Complete(CreateStep.Completed);
            return Task.CompletedTask;
        }

        public Task HandleAsync(SendVerifyFailure message)
        {
            Complete(CreateStep.Failed, message.Reason);
            return Task.CompletedTask;
        }

        public async Task HandleAsync(SoftDeleteDoctorCompleted message, IMessageBus bus)
        {
            await bus.SendAsync(new SoftDeleteCommand { SagaId = SagaId, AccountId = AccountId });
        }

        public async Task HandleAsync(SoftDeleteDoctorFailure message, IMessageBus bus)
        {
            FailureReason = $"{FailureReason} (profile compensation failed: {message.Reason})";

            await bus.SendAsync(new SoftDeleteCommand { SagaId = SagaId, AccountId = AccountId });
        }

        public Task HandleAsync(SoftDeleteCompleted message)
        {
            Complete(CreateStep.Failed, FailureReason);
            return Task.CompletedTask;
        }

        public Task HandleAsync(SoftDeleteFailure message)
        {
            Complete(
                CreateStep.Failed,
                $"{FailureReason} (account compensation failed: {message.Reason})"
            );
            return Task.CompletedTask;
        }

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

        private async Task CompensateAsync(IMessageBus bus, string reason)
        {
            FailureReason = reason;
            Step = CreateStep.Compensating;
            TryDeleteTempAvatar();

            if (DoctorId != Guid.Empty)
            {
                await bus.SendAsync(
                    new SoftDeleteDoctorCommand { SagaId = SagaId, DoctorId = DoctorId }
                );
                return;
            }

            if (AccountId != Guid.Empty)
            {
                await bus.SendAsync(
                    new SoftDeleteCommand { SagaId = SagaId, AccountId = AccountId }
                );
                return;
            }

            Complete(CreateStep.Failed, reason);
        }

        /// <summary>Moves the saga to a terminal state and lets Wolverine delete it.</summary>
        private void Complete(CreateStep step, string? reason = null)
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
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }

            TempAvatarPath = null;
        }
    }
}
