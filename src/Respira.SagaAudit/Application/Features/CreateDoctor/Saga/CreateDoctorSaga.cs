using Application.Features.Authentication.CreateUser.Commands;
using Application.Features.Authentication.CreateUser.Events;
using Application.Features.Authentication.CreateUser.Rollback.Commands;
using Application.Features.Authentication.CreateUser.Rollback.Events;
using Application.Features.Doctors.Create.Commands;
using Application.Features.Doctors.Create.Events;
using Application.Features.Doctors.Rollback.Commands.CreateDoctor;
using Application.Features.Doctors.Rollback.Events;
using Application.Features.Doctors.Update.Commands;
using Application.Features.Doctors.Update.Events;
using Application.Features.Media.Create.Commands;
using Application.Features.Media.Create.Events;
using Application.Features.Media.Remove.Commands;
using Microsoft.Extensions.Logging;
using Respira.SagaAudit.Application.Features.CreateDoctor.Commands;

namespace Respira.SagaAudit.Application.Features.CreateDoctor.Saga
{
    /// <summary>
    /// Long-running saga that provisions a new doctor on behalf of a manager/admin doctor.
    /// Flow: Auth (create account with RoleType) -> Doctor (create profile) -> Media (avatar upload)
    /// -> Doctor (link the uploaded avatar to the profile). Any step failure compensates the
    /// already-completed steps in reverse order.
    /// </summary>
    public class CreateDoctorSaga : Wolverine.Saga
    {
        public Guid Id { get; set; }

        // Manager/admin who initiated the saga
        public Guid ManagerDoctorId { get; set; }

        // Doctor profile used to build the CreateDoctorCommand
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public List<Domain.Enums.DegreeType> Degrees { get; set; } = new();
        public Domain.Enums.AcademicTitleEnum AcademicTitle { get; set; }
        public Domain.Enums.PositionType Position { get; set; }
        public Domain.Enums.GenderType Gender { get; set; }
        public string CitizenIdentificationNumber { get; set; } = string.Empty;
        public DateTimeOffset? DateOfBirth { get; set; }
        public string Address { get; set; } = string.Empty;

        // Avatar used to build the CreateMediaCommand
        public string MediaFileName { get; set; } = string.Empty;
        public string MediaContentType { get; set; } = string.Empty;
        public long MediaSize { get; set; }
        public byte[] MediaData { get; set; } = Array.Empty<byte>();

        // Created resource identifiers
        public Guid? AuthUserId { get; set; }
        public Guid? DoctorId { get; set; }
        public Guid MediaId { get; set; }

        public static (CreateDoctorSaga, CreateAuthDoctorCommand) Start(
            CreateDoctorByManagerCommand cmd,
            ILogger<CreateDoctorSaga> logger
        )
        {
            var mediaId = Guid.NewGuid();
            var saga = new CreateDoctorSaga
            {
                Id = Guid.NewGuid(),
                ManagerDoctorId = cmd.ManagerDoctorId,
                MediaId = mediaId,
                FirstName = cmd.FirstName,
                LastName = cmd.LastName,
                Degrees = cmd.Degrees,
                AcademicTitle = cmd.AcademicTitle,
                Position = cmd.Position,
                Gender = cmd.Gender,
                CitizenIdentificationNumber = cmd.CitizenIdentificationNumber,
                DateOfBirth = cmd.DateOfBirth,
                Address = cmd.Address,
                MediaFileName = cmd.MediaFileName,
                MediaContentType = cmd.MediaContentType,
                MediaSize = cmd.MediaSize,
                MediaData = cmd.MediaData,
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

        public CreateDoctorCommand Handle(
            CreateAuthDoctorSuccess success,
            ILogger<CreateDoctorSaga> logger
        )
        {
            AuthUserId = success.AuthUserId;
            logger.LogInformation("CreateDoctor saga {SagaId}: auth account created", Id);

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

        // ---- Auth failed -> nothing to compensate ----
        public void Handle(CreateAuthDoctorFailure failure, ILogger<CreateDoctorSaga> logger)
        {
            logger.LogWarning(
                "CreateDoctor saga {SagaId}: auth step failed - {Message}",
                Id,
                failure.Message
            );
            MarkCompleted();
        }

        // ---- Doctor succeeded -> Media ----
        public CreateMediaCommand Handle(
            CreateDoctorSuccess success,
            ILogger<CreateDoctorSaga> logger
        )
        {
            DoctorId = success.DoctorId;
            logger.LogInformation("CreateDoctor saga {SagaId}: doctor profile created", Id);

            return new CreateMediaCommand
            {
                SagaId = Id,
                MediaId = MediaId,
                FileName = MediaFileName,
                ContentType = MediaContentType,
                Size = MediaSize,
                Data = MediaData,
            };
        }

        // ---- Doctor failed -> roll back Auth ----
        public RollbackCreateAuthDoctorCommand Handle(
            CreateDoctorFailure failure,
            ILogger<CreateDoctorSaga> logger
        )
        {
            logger.LogWarning(
                "CreateDoctor saga {SagaId}: doctor step failed - {Message}",
                Id,
                failure.Message
            );
            MarkCompleted();
            return new RollbackCreateAuthDoctorCommand
            {
                SagaId = Id,
                AuthUserId = AuthUserId!.Value,
            };
        }

        // ---- Media succeeded -> link avatar to the doctor profile ----
        public UpdateDoctorCommand Handle(
            CreateMediaSuccess success,
            ILogger<CreateDoctorSaga> logger
        )
        {
            MediaId = success.MediaId;
            logger.LogInformation(
                "CreateDoctor saga {SagaId}: media uploaded, linking to doctor",
                Id
            );

            return new UpdateDoctorCommand
            {
                SagaId = Id,
                DoctorId = DoctorId!.Value,
                FirstName = FirstName,
                LastName = LastName,
                Degrees = Degrees,
                AcademicTitle = AcademicTitle,
                Position = Position,
                Gender = Gender,
                CitizenIdentificationNumber = CitizenIdentificationNumber,
                DateOfBirth = DateOfBirth,
                Address = Address,
                MediaId = success.MediaId,
                DoctorCreatorId = ManagerDoctorId,
            };
        }

        // ---- Media failed -> roll back Doctor then Auth ----
        public object[] Handle(CreateMediaFailure failure, ILogger<CreateDoctorSaga> logger)
        {
            logger.LogWarning(
                "CreateDoctor saga {SagaId}: media step failed - {Message}",
                Id,
                failure.Message
            );
            MarkCompleted();
            return new object[]
            {
                new RollbackCreateDoctorCommand { SagaId = Id, DoctorId = DoctorId!.Value },
                new RollbackCreateAuthDoctorCommand { SagaId = Id, AuthUserId = AuthUserId!.Value },
            };
        }

        // ---- Linking (doctor update) succeeded -> done ----
        public void Handle(UpdateDoctorSuccessEvent success, ILogger<CreateDoctorSaga> logger)
        {
            logger.LogInformation("CreateDoctor saga {SagaId}: completed (avatar linked)", Id);
            MarkCompleted();
        }

        // ---- Linking failed -> clean up Media, Doctor and Auth ----
        public object[] Handle(UpdateDoctorFailureEvent failure, ILogger<CreateDoctorSaga> logger)
        {
            logger.LogWarning("CreateDoctor saga {SagaId}: linking step failed - cleaning up", Id);
            MarkCompleted();
            return new object[]
            {
                new RemoveMediaCommand { SagaId = Id, MediaId = MediaId },
                new RollbackCreateDoctorCommand { SagaId = Id, DoctorId = DoctorId!.Value },
                new RollbackCreateAuthDoctorCommand { SagaId = Id, AuthUserId = AuthUserId!.Value },
            };
        }

        // ---- NotFound handlers ----
        public static void NotFound(
            CreateAuthDoctorSuccess msg,
            ILogger<CreateDoctorSaga> logger
        ) => logger.LogWarning("CreateDoctor saga not found for {SagaId}", msg.SagaId);

        public static void NotFound(
            CreateAuthDoctorFailure msg,
            ILogger<CreateDoctorSaga> logger
        ) => logger.LogWarning("CreateDoctor saga not found for {SagaId}", msg.SagaId);

        public static void NotFound(CreateDoctorSuccess msg, ILogger<CreateDoctorSaga> logger) =>
            logger.LogWarning("CreateDoctor saga not found for {SagaId}", msg.SagaId);

        public static void NotFound(CreateDoctorFailure msg, ILogger<CreateDoctorSaga> logger) =>
            logger.LogWarning("CreateDoctor saga not found for {SagaId}", msg.SagaId);

        public static void NotFound(CreateMediaSuccess msg, ILogger<CreateDoctorSaga> logger) =>
            logger.LogWarning("CreateDoctor saga not found for {SagaId}", msg.SagaId);

        public static void NotFound(CreateMediaFailure msg, ILogger<CreateDoctorSaga> logger) =>
            logger.LogWarning("CreateDoctor saga not found for {SagaId}", msg.SagaId);

        public static void NotFound(
            UpdateDoctorSuccessEvent msg,
            ILogger<CreateDoctorSaga> logger
        ) => logger.LogWarning("CreateDoctor saga not found for {SagaId}", msg.SagaId);

        public static void NotFound(
            UpdateDoctorFailureEvent msg,
            ILogger<CreateDoctorSaga> logger
        ) => logger.LogWarning("CreateDoctor saga not found for {SagaId}", msg.SagaId);

        public static void NotFound(
            RollbackCreateDoctorSuccess msg,
            ILogger<CreateDoctorSaga> logger
        ) => logger.LogWarning("CreateDoctor saga not found for {SagaId}", msg.SagaId);

        public static void NotFound(
            RollbackCreateDoctorFailure msg,
            ILogger<CreateDoctorSaga> logger
        ) => logger.LogWarning("CreateDoctor saga not found for {SagaId}", msg.SagaId);

        public static void NotFound(
            RollbackCreateAuthDoctorSuccess msg,
            ILogger<CreateDoctorSaga> logger
        ) => logger.LogWarning("CreateDoctor saga not found for {SagaId}", msg.SagaId);

        public static void NotFound(
            RollbackCreateAuthDoctorFailure msg,
            ILogger<CreateDoctorSaga> logger
        ) => logger.LogWarning("CreateDoctor saga not found for {SagaId}", msg.SagaId);
    }
}
