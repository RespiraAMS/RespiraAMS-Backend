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

    public void Handle(UpdateAuthDoctorFailure failure, ILogger<UpdateDoctorSaga> logger)
    {
        logger.LogWarning("UpdateDoctor saga {SagaId}: auth step failed - {Message}", Id, failure.Message);
        MarkCompleted();
    }

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

    public void Handle(UpdateDoctorLinkAvatarSuccessEvent success, ILogger<UpdateDoctorSaga> logger)
    {
        logger.LogInformation("UpdateDoctor saga {SagaId}: completed (avatar linked)", Id);
        MarkCompleted();
    }

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

    public void Handle(RollbackUpdateAuthDoctorSuccess success, ILogger<UpdateDoctorSaga> logger)
    {
        logger.LogInformation("UpdateDoctor saga {SagaId}: auth rollback completed", Id);
        MarkCompleted();
    }

    public void Handle(RollbackUpdateAuthDoctorFailure failure, ILogger<UpdateDoctorSaga> logger)
    {
        logger.LogWarning("UpdateDoctor saga {SagaId}: auth rollback failed", Id);
        MarkCompleted();
    }

    public static void NotFound(UpdateAuthDoctorSuccess msg, ILogger<UpdateDoctorSaga> logger) =>
        logger.LogWarning("UpdateDoctor saga not found for {SagaId}", msg.SagaId);
    public static void NotFound(UpdateAuthDoctorFailure msg, ILogger<UpdateDoctorSaga> logger) =>
        logger.LogWarning("UpdateDoctor saga not found for {SagaId}", msg.SagaId);
    public static void NotFound(UpdateDoctorSuccessEvent msg, ILogger<UpdateDoctorSaga> logger) =>
        logger.LogWarning("UpdateDoctor saga not found for {SagaId}", msg.SagaId);
    public static void NotFound(UpdateDoctorFailureEvent msg, ILogger<UpdateDoctorSaga> logger) =>
        logger.LogWarning("UpdateDoctor saga not found for {SagaId}", msg.SagaId);
    public static void NotFound(UpdateDoctorLinkAvatarSuccessEvent msg, ILogger<UpdateDoctorSaga> logger) =>
        logger.LogWarning("UpdateDoctor saga not found for {SagaId}", msg.SagaId);
    public static void NotFound(UpdateDoctorLinkAvatarFailureEvent msg, ILogger<UpdateDoctorSaga> logger) =>
        logger.LogWarning("UpdateDoctor saga not found for {SagaId}", msg.SagaId);
    public static void NotFound(RollbackUpdateDoctorSuccess msg, ILogger<UpdateDoctorSaga> logger) =>
        logger.LogWarning("UpdateDoctor saga not found for {SagaId}", msg.SagaId);
    public static void NotFound(RollbackUpdateDoctorFailure msg, ILogger<UpdateDoctorSaga> logger) =>
        logger.LogWarning("UpdateDoctor saga not found for {SagaId}", msg.SagaId);
    public static void NotFound(RollbackUpdateAuthDoctorSuccess msg, ILogger<UpdateDoctorSaga> logger) =>
        logger.LogWarning("UpdateDoctor saga not found for {SagaId}", msg.SagaId);
    public static void NotFound(RollbackUpdateAuthDoctorFailure msg, ILogger<UpdateDoctorSaga> logger) =>
        logger.LogWarning("UpdateDoctor saga not found for {SagaId}", msg.SagaId);
}
