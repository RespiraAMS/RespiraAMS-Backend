using Application.Features.Authentication.UpdateUser.Commands;
using Application.Features.Authentication.UpdateUser.Events;
using Application.Features.Authentication.UpdateUser.Rollback.Commands;
using Application.Features.Authentication.UpdateUser.Rollback.Events;
using Application.Features.Doctors.LinkAvatar.Commands;
using Application.Features.Doctors.LinkAvatar.Events;
using Application.Features.Doctors.Rollback.Commands.UpdateDoctor;
using Application.Features.Doctors.Rollback.Events;
using Application.Features.Doctors.Update.Commands;
using Application.Features.Doctors.Update.Events;
using Application.Features.Media.Remove.Commands;
using Microsoft.Extensions.Logging;
using Respira.SagaAudit.Application.Features.UpdateDoctor.Commands;
using Wolverine;

namespace Respira.SagaAudit.Application.Features.UpdateDoctor.Saga;

/// <summary>
/// Orchestrates the update of an existing doctor: Auth → Doctor → LinkAvatar (optional).
/// Carries both new and old values to enable compensation on failure.
/// </summary>
public class UpdateDoctorSaga : Wolverine.Saga
{
    public Guid Id { get; set; }

    public Guid ManagerDoctorId { get; set; }
    public Guid AuthUserId { get; set; }
    public Guid DoctorId { get; set; }
    public Guid MediaId { get; set; }
    public bool HasNewMedia { get; set; }

    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public Domain.Enums.RoleType Role { get; set; }

    public string OldEmail { get; set; } = string.Empty;
    public string OldPhone { get; set; } = string.Empty;
    public Domain.Enums.RoleType OldRole { get; set; }

    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public List<Domain.Enums.DegreeType> Degrees { get; set; } = new();
    public Domain.Enums.AcademicTitleEnum AcademicTitle { get; set; }
    public Domain.Enums.PositionType Position { get; set; }
    public Domain.Enums.GenderType Gender { get; set; }
    public string CitizenIdentificationNumber { get; set; } = string.Empty;
    public DateTimeOffset? DateOfBirth { get; set; }
    public string Address { get; set; } = string.Empty;

    public string OldFirstName { get; set; } = string.Empty;
    public string OldLastName { get; set; } = string.Empty;
    public List<Domain.Enums.DegreeType> OldDegrees { get; set; } = new();
    public Domain.Enums.AcademicTitleEnum OldAcademicTitle { get; set; }
    public Domain.Enums.PositionType OldPosition { get; set; }
    public Domain.Enums.GenderType OldGender { get; set; }
    public string OldCitizenIdentificationNumber { get; set; } = string.Empty;
    public DateTimeOffset? OldDateOfBirth { get; set; }
    public string OldAddress { get; set; } = string.Empty;

    public Guid? NewMediaId { get; set; }

    /// <summary>
    /// Initializes the saga state (new and old values) from the manager command and
    /// emits the first <see cref="UpdateAuthDoctorCommand"/> to update the auth account.
    /// </summary>
    /// <param name="cmd">The manager-initiated command that started the saga.</param>
    /// <param name="logger">Logger used to record saga start.</param>
    /// <returns>The initialized saga and the first auth update command to dispatch.</returns>
    public static (UpdateDoctorSaga, UpdateAuthDoctorCommand) Start(
        UpdateDoctorByManagerCommand cmd,
        ILogger<UpdateDoctorSaga> logger)
    {
        var saga = new UpdateDoctorSaga
        {
            Id = Guid.NewGuid(),
            ManagerDoctorId = cmd.ManagerDoctorId,
            AuthUserId = cmd.AuthUserId,
            DoctorId = cmd.DoctorId,
            MediaId = cmd.MediaId,
            HasNewMedia = cmd.HasNewMedia,
            Email = cmd.Email,
            Phone = cmd.Phone,
            Role = cmd.Role,
            OldEmail = cmd.OldEmail,
            OldPhone = cmd.OldPhone,
            OldRole = cmd.OldRole,
            FirstName = cmd.FirstName,
            LastName = cmd.LastName,
            Degrees = cmd.Degrees,
            AcademicTitle = cmd.AcademicTitle,
            Position = cmd.Position,
            Gender = cmd.Gender,
            CitizenIdentificationNumber = cmd.CitizenIdentificationNumber,
            DateOfBirth = cmd.DateOfBirth,
            Address = cmd.Address,
            OldFirstName = cmd.OldFirstName,
            OldLastName = cmd.OldLastName,
            OldDegrees = cmd.OldDegrees,
            OldAcademicTitle = cmd.OldAcademicTitle,
            OldPosition = cmd.OldPosition,
            OldGender = cmd.OldGender,
            OldCitizenIdentificationNumber = cmd.OldCitizenIdentificationNumber,
            OldDateOfBirth = cmd.OldDateOfBirth,
            OldAddress = cmd.OldAddress,
            NewMediaId = cmd.NewMediaId,
        };

        logger.LogInformation("UpdateDoctor saga {SagaId} started by manager {ManagerId}", saga.Id, cmd.ManagerDoctorId);

        var authCommand = new UpdateAuthDoctorCommand
        {
            SagaId = saga.Id,
            AuthUserId = saga.AuthUserId,
            Email = saga.Email,
            Phone = saga.Phone,
            Role = saga.Role,
            OldEmail = saga.OldEmail,
            OldPhone = saga.OldPhone,
            OldRole = saga.OldRole,
        };

        return (saga, authCommand);
    }

    /// <summary>
    /// Step 1 — auth updated; emits the <see cref="UpdateDoctorCommand"/> to update
    /// the doctor profile.
    /// </summary>
    /// <param name="success">Confirmation that the auth account was updated.</param>
    /// <param name="logger">Logger used to record step progress.</param>
    /// <returns>The command that updates the doctor profile.</returns>
    public UpdateDoctorCommand Handle(UpdateAuthDoctorSuccess success, ILogger<UpdateDoctorSaga> logger)
    {
        logger.LogInformation("UpdateDoctor saga {SagaId}: auth updated", Id);
        return new UpdateDoctorCommand
        {
            SagaId = Id,
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
            MediaId = MediaId,
            DoctorCreatorId = ManagerDoctorId,
        };
    }

    /// <summary>
    /// Auth step failed. Marks the saga completed; no compensation is needed since
    /// the doctor profile had not yet been touched.
    /// </summary>
    /// <param name="failure">Details of the auth failure.</param>
    /// <param name="logger">Logger used to record the failure.</param>
    public void Handle(UpdateAuthDoctorFailure failure, ILogger<UpdateDoctorSaga> logger)
    {
        logger.LogWarning("UpdateDoctor saga {SagaId}: auth step failed - {Message}", Id, failure.Message);
        MarkCompleted();
    }

    /// <summary>
    /// Step 2 — doctor profile updated. If a new avatar was requested, emits the
    /// <see cref="UpdateDoctorLinkAvatarCommand"/>; otherwise the saga completes.
    /// </summary>
    /// <param name="success">Confirmation that the doctor profile was updated.</param>
    /// <param name="logger">Logger used to record step progress.</param>
    /// <returns>The avatar link command, or an empty array when no new avatar is needed.</returns>
    public object[] Handle(UpdateDoctorSuccessEvent success, ILogger<UpdateDoctorSaga> logger)
    {
        logger.LogInformation("UpdateDoctor saga {SagaId}: doctor updated", Id);
        if (!HasNewMedia || NewMediaId is null)
        {
            MarkCompleted();
            return Array.Empty<object>();
        }

        return new object[]
        {
            new UpdateDoctorLinkAvatarCommand
            {
                SagaId = Id,
                DoctorId = DoctorId,
                MediaId = NewMediaId.Value,
            },
        };
    }

    /// <summary>
    /// Doctor step failed. Triggers compensation by emitting a
    /// <see cref="RollbackUpdateAuthDoctorCommand"/> to revert the auth account to
    /// its previous values, then marks the saga completed (terminal).
    /// </summary>
    /// <param name="failure">Details of the doctor update failure.</param>
    /// <param name="logger">Logger used to record the failure.</param>
    /// <returns>The command that rolls back the auth account.</returns>
    public RollbackUpdateAuthDoctorCommand Handle(UpdateDoctorFailureEvent failure, ILogger<UpdateDoctorSaga> logger)
    {
        logger.LogWarning("UpdateDoctor saga {SagaId}: doctor step failed - reverting auth", Id);
        MarkCompleted();
        return new RollbackUpdateAuthDoctorCommand
        {
            SagaId = Id,
            AuthUserId = AuthUserId,
            OldEmail = OldEmail,
            OldPhone = OldPhone,
            OldRole = OldRole,
        };
    }

    /// <summary>
    /// Final step — avatar linked successfully. Marks the saga completed.
    /// </summary>
    /// <param name="success">Confirmation that the avatar was linked.</param>
    /// <param name="logger">Logger used to record completion.</param>
    public void Handle(UpdateDoctorLinkAvatarSuccessEvent success, ILogger<UpdateDoctorSaga> logger)
    {
        logger.LogInformation("UpdateDoctor saga {SagaId}: completed (avatar linked)", Id);
        MarkCompleted();
    }

    /// <summary>
    /// Avatar linking failed. Triggers compensation in reverse order by emitting
    /// commands to remove the new media, revert the doctor profile to its previous
    /// values, and revert the auth account; then marks the saga completed (terminal).
    /// </summary>
    /// <param name="failure">Details of the avatar linking failure.</param>
    /// <param name="logger">Logger used to record the cleanup.</param>
    /// <returns>The compensation commands (RemoveMedia, RollbackDoctor, RollbackAuth).</returns>
    public object[] Handle(UpdateDoctorLinkAvatarFailureEvent failure, ILogger<UpdateDoctorSaga> logger)
    {
        logger.LogWarning("UpdateDoctor saga {SagaId}: linking step failed - cleaning up", Id);
        return new object[]
        {
            new RemoveMediaCommand { SagaId = Id, MediaId = NewMediaId!.Value },
            new RollbackUpdateDoctorCommand
            {
                SagaId = Id,
                DoctorId = DoctorId,
                FirstName = OldFirstName,
                LastName = OldLastName,
                Degrees = OldDegrees,
                AcademicTitle = OldAcademicTitle,
                Position = OldPosition,
                Gender = OldGender,
                CitizenIdentificationNumber = OldCitizenIdentificationNumber,
                DateOfBirth = OldDateOfBirth,
                Address = OldAddress,
                MediaId = MediaId,
                DoctorCreatorId = ManagerDoctorId,
            },
            new RollbackUpdateAuthDoctorCommand
            {
                SagaId = Id,
                AuthUserId = AuthUserId,
                OldEmail = OldEmail,
                OldPhone = OldPhone,
                OldRole = OldRole,
            },
        };
    }

    /// <summary>
    /// Auth rollback completed after a failed doctor update. Marks the saga completed.
    /// </summary>
    /// <param name="success">Confirmation that the auth rollback succeeded.</param>
    /// <param name="logger">Logger used to record completion.</param>
    public void Handle(RollbackUpdateAuthDoctorSuccess success, ILogger<UpdateDoctorSaga> logger)
    {
        logger.LogInformation("UpdateDoctor saga {SagaId}: auth rollback completed", Id);
        MarkCompleted();
    }

    /// <summary>
    /// Auth rollback failed after a failed doctor update. Marks the saga completed.
    /// </summary>
    /// <param name="failure">Details of the auth rollback failure.</param>
    /// <param name="logger">Logger used to record the failure.</param>
    public void Handle(RollbackUpdateAuthDoctorFailure failure, ILogger<UpdateDoctorSaga> logger)
    {
        logger.LogWarning("UpdateDoctor saga {SagaId}: auth rollback failed", Id);
        MarkCompleted();
    }

    /// <summary>Wolverine fallback when no running saga matches the incoming message; logs a warning.</summary>
    public static void NotFound(UpdateAuthDoctorSuccess msg, ILogger<UpdateDoctorSaga> logger) =>
        logger.LogWarning("UpdateDoctor saga not found for {SagaId}", msg.SagaId);
    /// <summary>Wolverine fallback when no running saga matches the incoming message; logs a warning.</summary>
    public static void NotFound(UpdateAuthDoctorFailure msg, ILogger<UpdateDoctorSaga> logger) =>
        logger.LogWarning("UpdateDoctor saga not found for {SagaId}", msg.SagaId);
    /// <summary>Wolverine fallback when no running saga matches the incoming message; logs a warning.</summary>
    public static void NotFound(UpdateDoctorSuccessEvent msg, ILogger<UpdateDoctorSaga> logger) =>
        logger.LogWarning("UpdateDoctor saga not found for {SagaId}", msg.SagaId);
    /// <summary>Wolverine fallback when no running saga matches the incoming message; logs a warning.</summary>
    public static void NotFound(UpdateDoctorFailureEvent msg, ILogger<UpdateDoctorSaga> logger) =>
        logger.LogWarning("UpdateDoctor saga not found for {SagaId}", msg.SagaId);
    /// <summary>Wolverine fallback when no running saga matches the incoming message; logs a warning.</summary>
    public static void NotFound(UpdateDoctorLinkAvatarSuccessEvent msg, ILogger<UpdateDoctorSaga> logger) =>
        logger.LogWarning("UpdateDoctor saga not found for {SagaId}", msg.SagaId);
    /// <summary>Wolverine fallback when no running saga matches the incoming message; logs a warning.</summary>
    public static void NotFound(UpdateDoctorLinkAvatarFailureEvent msg, ILogger<UpdateDoctorSaga> logger) =>
        logger.LogWarning("UpdateDoctor saga not found for {SagaId}", msg.SagaId);
    /// <summary>Wolverine fallback when no running saga matches the incoming message; logs a warning.</summary>
    public static void NotFound(RollbackUpdateDoctorSuccess msg, ILogger<UpdateDoctorSaga> logger) =>
        logger.LogWarning("UpdateDoctor saga not found for {SagaId}", msg.SagaId);
    /// <summary>Wolverine fallback when no running saga matches the incoming message; logs a warning.</summary>
    public static void NotFound(RollbackUpdateDoctorFailure msg, ILogger<UpdateDoctorSaga> logger) =>
        logger.LogWarning("UpdateDoctor saga not found for {SagaId}", msg.SagaId);
    /// <summary>Wolverine fallback when no running saga matches the incoming message; logs a warning.</summary>
    public static void NotFound(RollbackUpdateAuthDoctorSuccess msg, ILogger<UpdateDoctorSaga> logger) =>
        logger.LogWarning("UpdateDoctor saga not found for {SagaId}", msg.SagaId);
    /// <summary>Wolverine fallback when no running saga matches the incoming message; logs a warning.</summary>
    public static void NotFound(RollbackUpdateAuthDoctorFailure msg, ILogger<UpdateDoctorSaga> logger) =>
        logger.LogWarning("UpdateDoctor saga not found for {SagaId}", msg.SagaId);
}
