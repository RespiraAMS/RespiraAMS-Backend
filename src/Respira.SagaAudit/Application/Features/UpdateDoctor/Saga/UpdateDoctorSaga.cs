using Application.Features.Authentication.UpdateUser.Commands;
using Application.Features.Authentication.UpdateUser.Events;
using Application.Features.Authentication.UpdateUser.Rollback.Commands;
using Application.Features.Authentication.UpdateUser.Rollback.Events;
using Application.Features.Doctors.Rollback.Commands.UpdateDoctor;
using Application.Features.Doctors.Rollback.Events;
using Application.Features.Doctors.Update.Commands;
using Application.Features.Doctors.Update.Events;
using Application.Features.Media.Update.Commands;
using Application.Features.Media.Update.Events;
using Microsoft.Extensions.Logging;
using Respira.SagaAudit.Application.Features.UpdateDoctor.Commands;
using Wolverine;

namespace Respira.SagaAudit.Application.Features.UpdateDoctor.Saga;

/// <summary>
/// Long-running saga that updates a doctor on behalf of a manager/admin doctor.
/// Flow: Auth -> Doctor -> Media (avatar, optional). Any step failure reverts the
/// already-applied steps to their previous values.
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

    public string? NewMediaFileName { get; set; }
    public string? NewMediaContentType { get; set; }
    public long NewMediaSize { get; set; }
    public byte[]? NewMediaData { get; set; }

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
            NewMediaFileName = cmd.NewMediaFileName,
            NewMediaContentType = cmd.NewMediaContentType,
            NewMediaSize = cmd.NewMediaSize,
            NewMediaData = cmd.NewMediaData,
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
        if (!HasNewMedia || NewMediaData is null)
        {
            MarkCompleted();
            return Array.Empty<object>();
        }

        return new object[]
        {
            new UpdateMediaCommand
            {
                SagaId = Id,
                MediaId = MediaId,
                FileName = NewMediaFileName!,
                ContentType = NewMediaContentType!,
                Size = NewMediaSize,
                Data = NewMediaData,
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

    public void Handle(UpdateMediaSuccess success, ILogger<UpdateDoctorSaga> logger)
    {
        logger.LogInformation("UpdateDoctor saga {SagaId}: completed (media {MediaId})", Id, success.MediaId);
        MarkCompleted();
    }

    public object[] Handle(UpdateMediaFailure failure, ILogger<UpdateDoctorSaga> logger)
    {
        logger.LogWarning("UpdateDoctor saga {SagaId}: media step failed - reverting doctor & auth", Id);
        MarkCompleted();
        return new object[]
        {
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

    public static void NotFound(UpdateAuthDoctorSuccess msg, ILogger<UpdateDoctorSaga> logger) =>
        logger.LogWarning("UpdateDoctor saga not found for {SagaId}", msg.SagaId);
    public static void NotFound(UpdateAuthDoctorFailure msg, ILogger<UpdateDoctorSaga> logger) =>
        logger.LogWarning("UpdateDoctor saga not found for {SagaId}", msg.SagaId);
    public static void NotFound(UpdateDoctorSuccessEvent msg, ILogger<UpdateDoctorSaga> logger) =>
        logger.LogWarning("UpdateDoctor saga not found for {SagaId}", msg.SagaId);
    public static void NotFound(UpdateDoctorFailureEvent msg, ILogger<UpdateDoctorSaga> logger) =>
        logger.LogWarning("UpdateDoctor saga not found for {SagaId}", msg.SagaId);
    public static void NotFound(UpdateMediaSuccess msg, ILogger<UpdateDoctorSaga> logger) =>
        logger.LogWarning("UpdateDoctor saga not found for {SagaId}", msg.SagaId);
    public static void NotFound(UpdateMediaFailure msg, ILogger<UpdateDoctorSaga> logger) =>
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
