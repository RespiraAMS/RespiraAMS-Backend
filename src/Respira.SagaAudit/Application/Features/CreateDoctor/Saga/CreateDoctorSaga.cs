using Application.Features.Authentication.CreateUser.Commands;
using Application.Features.Authentication.CreateUser.Events;
using Application.Features.Authentication.CreateUser.Rollback.Commands;
using Application.Features.Authentication.CreateUser.Rollback.Events;
using Application.Features.Doctors.Create.Commands;
using Application.Features.Doctors.Create.Events;
using Application.Features.Doctors.Rollback.Commands.CreateDoctor;
using Application.Features.Doctors.Rollback.Events;
using Application.Features.Doctors.LinkAvatar.Commands;
using Application.Features.Doctors.LinkAvatar.Events;
using Application.Features.Media.Remove.Commands;
using Microsoft.Extensions.Logging;
using Respira.SagaAudit.Application.Features.CreateDoctor.Commands;
using Respira.SagaAudit.Application.Services;

namespace Respira.SagaAudit.Application.Features.CreateDoctor.Saga
{
    /// <summary>
    /// Orchestrates the creation of a new doctor: Auth → Doctor → LinkAvatar.
    /// Each step failure triggers compensation in reverse order.
    /// </summary>
    public class CreateDoctorSaga : Wolverine.Saga
    {
        public Guid Id { get; set; }
        public Guid ManagerDoctorId { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public List<Domain.Enums.DegreeType> Degrees { get; set; } = new();
        public Domain.Enums.AcademicTitleEnum AcademicTitle { get; set; }
        public Domain.Enums.PositionType Position { get; set; }
        public Domain.Enums.GenderType Gender { get; set; }
        public string CitizenIdentificationNumber { get; set; } = string.Empty;
        public DateTimeOffset? DateOfBirth { get; set; }
        public string Address { get; set; } = string.Empty;
        public Guid? AuthUserId { get; set; }
        public Guid? DoctorId { get; set; }
        public Guid MediaId { get; set; }

        /// <summary>
        /// Initializes the saga state from the manager command and emits the first
        /// <see cref="CreateAuthDoctorCommand"/> to provision the auth account.
        /// </summary>
        /// <param name="cmd">The manager-initiated command that started the saga.</param>
        /// <param name="logger">Logger used to record saga start.</param>
        /// <returns>The initialized saga and the first auth command to dispatch.</returns>
        public static (CreateDoctorSaga, CreateAuthDoctorCommand) Start(
            CreateDoctorByManagerCommand cmd,
            ILogger<CreateDoctorSaga> logger
        )
        {
            var saga = new CreateDoctorSaga
            {
                Id = cmd.SagaId,
                ManagerDoctorId = cmd.ManagerDoctorId,
                MediaId = cmd.MediaId,
                FirstName = cmd.FirstName,
                LastName = cmd.LastName,
                Degrees = cmd.Degrees,
                AcademicTitle = cmd.AcademicTitle,
                Position = cmd.Position,
                Gender = cmd.Gender,
                CitizenIdentificationNumber = cmd.CitizenIdentificationNumber,
                DateOfBirth = cmd.DateOfBirth,
                Address = cmd.Address,
            };

            logger.LogInformation(
                "CreateDoctor saga {SagaId} started by manager {ManagerId} for role {Role}",
                saga.Id,
                cmd.ManagerDoctorId,
                cmd.Role
            );

            var authCommand = new CreateAuthDoctorCommand
            {
                SagaId = saga.Id,
                AuthUserId = Guid.NewGuid(),
                Email = cmd.Email,
                Password = cmd.Password,
                Phone = cmd.Phone,
                Role = cmd.Role,
            };

            return (saga, authCommand);
        }

        /// <summary>
        /// Step 1 — records the created auth user and emits the
        /// <see cref="CreateDoctorCommand"/> to create the doctor profile.
        /// </summary>
        /// <param name="success">Confirmation that the auth account was created.</param>
        /// <param name="logger">Logger used to record step progress.</param>
        /// <param name="tracker">Process tracker used to record the Auth:Created step.</param>
        /// <returns>The command that creates the doctor profile.</returns>
        public async Task<CreateDoctorCommand> Handle(
            CreateAuthDoctorSuccess success,
            ILogger<CreateDoctorSaga> logger,
            ProcessTrackerService tracker
        )
        {
            AuthUserId = success.AuthUserId;
            logger.LogInformation("CreateDoctor saga {SagaId}: auth account created", Id);
            await tracker.UpdateStepAsync(Id, "Auth:Created");

            return new CreateDoctorCommand
            {
                SagaId = Id,
                DoctorId = Guid.NewGuid(),
                FirstName = FirstName,
                LastName = LastName,
                Degrees = Degrees,
                AcademicTitle = AcademicTitle,
                Position = Position,
                Gender = Gender,
                CitizenIdentificationNumber = CitizenIdentificationNumber,
                DateOfBirth = DateOfBirth,
                Address = Address,
                DoctorCreatorId = ManagerDoctorId,
            };
        }

        /// <summary>
        /// Auth step failed. Marks the saga as failed and completed; no compensation
        /// is needed since nothing had been created yet.
        /// </summary>
        /// <param name="failure">Details of the auth failure.</param>
        /// <param name="logger">Logger used to record the failure.</param>
        /// <param name="tracker">Process tracker used to record the failure.</param>
        public async Task Handle(
            CreateAuthDoctorFailure failure,
            ILogger<CreateDoctorSaga> logger,
            ProcessTrackerService tracker
        )
        {
            logger.LogWarning(
                "CreateDoctor saga {SagaId}: auth step failed - {Message}",
                Id,
                failure.Message
            );
            await tracker.FailAsync(Id, $"Auth failed: {failure.Message}");
            MarkCompleted();
        }

        /// <summary>
        /// Step 2 — records the created doctor and emits the
        /// <see cref="LinkDoctorAvatarCommand"/> to link the uploaded avatar.
        /// </summary>
        /// <param name="success">Confirmation that the doctor profile was created.</param>
        /// <param name="logger">Logger used to record step progress.</param>
        /// <param name="tracker">Process tracker used to record the Doctor:Created step.</param>
        /// <returns>The command that links the avatar to the doctor.</returns>
        public async Task<LinkDoctorAvatarCommand> Handle(
            CreateDoctorSuccess success,
            ILogger<CreateDoctorSaga> logger,
            ProcessTrackerService tracker
        )
        {
            DoctorId = success.DoctorId;
            logger.LogInformation("CreateDoctor saga {SagaId}: doctor profile created", Id);
            await tracker.UpdateStepAsync(Id, "Doctor:Created");

            return new LinkDoctorAvatarCommand
            {
                SagaId = Id,
                DoctorId = DoctorId!.Value,
                MediaId = MediaId,
            };
        }

        /// <summary>
        /// Doctor step failed. Triggers compensation by emitting a
        /// <see cref="RollbackCreateAuthDoctorCommand"/> to remove the auth account
        /// (reverse order), then marks the saga completed (terminal).
        /// </summary>
        /// <param name="failure">Details of the doctor creation failure.</param>
        /// <param name="logger">Logger used to record the failure.</param>
        /// <param name="tracker">Process tracker used to record compensation.</param>
        /// <returns>The command that rolls back the auth account.</returns>
        public async Task<RollbackCreateAuthDoctorCommand> Handle(
            CreateDoctorFailure failure,
            ILogger<CreateDoctorSaga> logger,
            ProcessTrackerService tracker
        )
        {
            logger.LogWarning(
                "CreateDoctor saga {SagaId}: doctor step failed - {Message}",
                Id,
                failure.Message
            );
            await tracker.CompensateAsync(Id);
            MarkCompleted();
            return new RollbackCreateAuthDoctorCommand
            {
                SagaId = Id,
                AuthUserId = AuthUserId!.Value,
            };
        }

        /// <summary>
        /// Final step — avatar linked successfully. Marks the saga as completed.
        /// </summary>
        /// <param name="success">Confirmation that the avatar was linked.</param>
        /// <param name="logger">Logger used to record completion.</param>
        /// <param name="tracker">Process tracker used to record completion.</param>
        public async Task Handle(
            LinkDoctorAvatarSuccessEvent success,
            ILogger<CreateDoctorSaga> logger,
            ProcessTrackerService tracker
        )
        {
            logger.LogInformation("CreateDoctor saga {SagaId}: completed (avatar linked)", Id);
            await tracker.CompleteAsync(Id);
            MarkCompleted();
        }

        /// <summary>
        /// Avatar linking failed. Triggers compensation in reverse order by emitting
        /// commands to remove the media, roll back the doctor profile, and roll back
        /// the auth account; then marks the saga completed (terminal).
        /// </summary>
        /// <param name="failure">Details of the avatar linking failure.</param>
        /// <param name="logger">Logger used to record the cleanup.</param>
        /// <param name="tracker">Process tracker used to record compensation.</param>
        /// <returns>The compensation commands (RemoveMedia, RollbackDoctor, RollbackAuth).</returns>
        public async Task<object[]> Handle(
            LinkDoctorAvatarFailureEvent failure,
            ILogger<CreateDoctorSaga> logger,
            ProcessTrackerService tracker
        )
        {
            logger.LogWarning("CreateDoctor saga {SagaId}: linking step failed - cleaning up", Id);
            await tracker.CompensateAsync(Id);
            return new object[]
            {
                new RemoveMediaCommand { SagaId = Id, MediaId = MediaId },
                new RollbackCreateDoctorCommand { SagaId = Id, DoctorId = DoctorId!.Value },
                new RollbackCreateAuthDoctorCommand { SagaId = Id, AuthUserId = AuthUserId!.Value },
            };
        }

        /// <summary>
        /// Auth rollback completed after a failed avatar link. Marks the saga as
        /// failed (compensation completed).
        /// </summary>
        /// <param name="success">Confirmation that the auth rollback succeeded.</param>
        /// <param name="logger">Logger used to record completion.</param>
        /// <param name="tracker">Process tracker used to record the failure.</param>
        public async Task Handle(
            RollbackCreateAuthDoctorSuccess success,
            ILogger<CreateDoctorSaga> logger,
            ProcessTrackerService tracker
        )
        {
            logger.LogInformation("CreateDoctor saga {SagaId}: auth rollback completed", Id);
            await tracker.FailAsync(Id, "LinkAvatar failed, compensation completed");
            MarkCompleted();
        }

        /// <summary>
        /// Auth rollback failed after a failed avatar link. Marks the saga as failed
        /// (compensation also failed).
        /// </summary>
        /// <param name="failure">Details of the auth rollback failure.</param>
        /// <param name="logger">Logger used to record the failure.</param>
        /// <param name="tracker">Process tracker used to record the failure.</param>
        public async Task Handle(
            RollbackCreateAuthDoctorFailure failure,
            ILogger<CreateDoctorSaga> logger,
            ProcessTrackerService tracker
        )
        {
            logger.LogWarning("CreateDoctor saga {SagaId}: auth rollback failed", Id);
            await tracker.FailAsync(Id, "LinkAvatar failed, compensation also failed");
            MarkCompleted();
        }

        /// <summary>Wolverine fallback when no running saga matches the incoming message; logs a warning.</summary>
        public static void NotFound(
            CreateAuthDoctorSuccess msg,
            ILogger<CreateDoctorSaga> logger
        ) => logger.LogWarning("CreateDoctor saga not found for {SagaId}", msg.SagaId);

        /// <summary>Wolverine fallback when no running saga matches the incoming message; logs a warning.</summary>
        public static void NotFound(
            CreateAuthDoctorFailure msg,
            ILogger<CreateDoctorSaga> logger
        ) => logger.LogWarning("CreateDoctor saga not found for {SagaId}", msg.SagaId);

        /// <summary>Wolverine fallback when no running saga matches the incoming message; logs a warning.</summary>
        public static void NotFound(CreateDoctorSuccess msg, ILogger<CreateDoctorSaga> logger) =>
            logger.LogWarning("CreateDoctor saga not found for {SagaId}", msg.SagaId);

        /// <summary>Wolverine fallback when no running saga matches the incoming message; logs a warning.</summary>
        public static void NotFound(CreateDoctorFailure msg, ILogger<CreateDoctorSaga> logger) =>
            logger.LogWarning("CreateDoctor saga not found for {SagaId}", msg.SagaId);

        /// <summary>Wolverine fallback when no running saga matches the incoming message; logs a warning.</summary>
        public static void NotFound(
            LinkDoctorAvatarSuccessEvent msg,
            ILogger<CreateDoctorSaga> logger
        ) => logger.LogWarning("CreateDoctor saga not found for {SagaId}", msg.SagaId);

        /// <summary>Wolverine fallback when no running saga matches the incoming message; logs a warning.</summary>
        public static void NotFound(
            LinkDoctorAvatarFailureEvent msg,
            ILogger<CreateDoctorSaga> logger
        ) => logger.LogWarning("CreateDoctor saga not found for {SagaId}", msg.SagaId);

        /// <summary>Wolverine fallback when no running saga matches the incoming message; logs a warning.</summary>
        public static void NotFound(
            RollbackCreateDoctorSuccess msg,
            ILogger<CreateDoctorSaga> logger
        ) => logger.LogWarning("CreateDoctor saga not found for {SagaId}", msg.SagaId);

        /// <summary>Wolverine fallback when no running saga matches the incoming message; logs a warning.</summary>
        public static void NotFound(
            RollbackCreateDoctorFailure msg,
            ILogger<CreateDoctorSaga> logger
        ) => logger.LogWarning("CreateDoctor saga not found for {SagaId}", msg.SagaId);

        /// <summary>Wolverine fallback when no running saga matches the incoming message; logs a warning.</summary>
        public static void NotFound(
            RollbackCreateAuthDoctorSuccess msg,
            ILogger<CreateDoctorSaga> logger
        ) => logger.LogWarning("CreateDoctor saga not found for {SagaId}", msg.SagaId);

        /// <summary>Wolverine fallback when no running saga matches the incoming message; logs a warning.</summary>
        public static void NotFound(
            RollbackCreateAuthDoctorFailure msg,
            ILogger<CreateDoctorSaga> logger
        ) => logger.LogWarning("CreateDoctor saga not found for {SagaId}", msg.SagaId);
    }
}
