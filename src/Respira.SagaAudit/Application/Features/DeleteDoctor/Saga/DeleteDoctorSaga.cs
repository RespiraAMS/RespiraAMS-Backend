using Application.Features.Authentication.DeleteUser.Commands;
using Application.Features.Authentication.DeleteUser.Events;
using Application.Features.Doctors.Delete.Commands;
using Application.Features.Doctors.Delete.Events;
using Application.Features.Media.Remove.Commands;
using Application.Features.Media.Remove.Events;
using Microsoft.Extensions.Logging;
using Respira.SagaAudit.Application.Features.DeleteDoctor.Commands;
using Wolverine;

namespace Respira.SagaAudit.Application.Features.DeleteDoctor.Saga;

/// <summary>
/// Long-running saga that deletes a doctor on behalf of a manager/admin doctor.
/// Flow (reverse of creation): Media (avatar) -> Doctor (profile) -> Auth (account).
/// Deletion is terminal, so failures are not compensated (the resources already
/// removed cannot be restored without their original password hash / file bytes).
/// </summary>
public class DeleteDoctorSaga : Wolverine.Saga
{
    public Guid Id { get; set; }

    public Guid ManagerDoctorId { get; set; }
    public Guid AuthUserId { get; set; }
    public Guid DoctorId { get; set; }
    public Guid MediaId { get; set; }

    public static (DeleteDoctorSaga, RemoveMediaCommand) Start(
        DeleteDoctorByManagerCommand cmd,
        ILogger<DeleteDoctorSaga> logger)
    {
        var saga = new DeleteDoctorSaga
        {
            Id = Guid.NewGuid(),
            ManagerDoctorId = cmd.ManagerDoctorId,
            AuthUserId = cmd.AuthUserId,
            DoctorId = cmd.DoctorId,
            MediaId = cmd.MediaId,
        };

        logger.LogInformation("DeleteDoctor saga {SagaId} started by manager {ManagerId}", saga.Id, cmd.ManagerDoctorId);

        return (saga, new RemoveMediaCommand
        {
            SagaId = saga.Id,
            MediaId = saga.MediaId,
        });
    }

    public DeleteDoctorCommand Handle(RemoveMediaSuccess success, ILogger<DeleteDoctorSaga> logger)
    {
        logger.LogInformation("DeleteDoctor saga {SagaId}: media removed", Id);
        return new DeleteDoctorCommand
        {
            SagaId = Id,
            DoctorId = DoctorId,
            DoctorCreatorId = ManagerDoctorId,
        };
    }

    public void Handle(RemoveMediaFailure failure, ILogger<DeleteDoctorSaga> logger)
    {
        logger.LogWarning("DeleteDoctor saga {SagaId}: media step failed - {Message}", Id, failure.Message);
        MarkCompleted();
    }

    public DeleteAuthDoctorCommand Handle(DeleteDoctorSuccess success, ILogger<DeleteDoctorSaga> logger)
    {
        logger.LogInformation("DeleteDoctor saga {SagaId}: doctor removed", Id);
        return new DeleteAuthDoctorCommand
        {
            SagaId = Id,
            AuthUserId = AuthUserId,
        };
    }

    public void Handle(DeleteDoctorFailure failure, ILogger<DeleteDoctorSaga> logger)
    {
        logger.LogWarning("DeleteDoctor saga {SagaId}: doctor step failed - {Message}", Id, failure.Message);
        MarkCompleted();
    }

    public void Handle(DeleteAuthDoctorSuccess success, ILogger<DeleteDoctorSaga> logger)
    {
        logger.LogInformation("DeleteDoctor saga {SagaId}: completed (auth {AuthUserId} removed)", Id, success.AuthUserId);
        MarkCompleted();
    }

    public void Handle(DeleteAuthDoctorFailure failure, ILogger<DeleteDoctorSaga> logger)
    {
        logger.LogWarning("DeleteDoctor saga {SagaId}: auth step failed - {Message}", Id, failure.Message);
        MarkCompleted();
    }

    public static void NotFound(RemoveMediaSuccess msg, ILogger<DeleteDoctorSaga> logger) =>
        logger.LogWarning("DeleteDoctor saga not found for {SagaId}", msg.SagaId);
    public static void NotFound(RemoveMediaFailure msg, ILogger<DeleteDoctorSaga> logger) =>
        logger.LogWarning("DeleteDoctor saga not found for {SagaId}", msg.SagaId);
    public static void NotFound(DeleteDoctorSuccess msg, ILogger<DeleteDoctorSaga> logger) =>
        logger.LogWarning("DeleteDoctor saga not found for {SagaId}", msg.SagaId);
    public static void NotFound(DeleteDoctorFailure msg, ILogger<DeleteDoctorSaga> logger) =>
        logger.LogWarning("DeleteDoctor saga not found for {SagaId}", msg.SagaId);
    public static void NotFound(DeleteAuthDoctorSuccess msg, ILogger<DeleteDoctorSaga> logger) =>
        logger.LogWarning("DeleteDoctor saga not found for {SagaId}", msg.SagaId);
    public static void NotFound(DeleteAuthDoctorFailure msg, ILogger<DeleteDoctorSaga> logger) =>
        logger.LogWarning("DeleteDoctor saga not found for {SagaId}", msg.SagaId);
}
