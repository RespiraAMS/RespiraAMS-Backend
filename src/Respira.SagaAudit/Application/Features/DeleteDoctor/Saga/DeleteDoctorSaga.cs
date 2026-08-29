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
    // Shared entity ID — same for both Auth and Doctor tables
    public Guid EntityId { get; set; }
    public Guid MediaId { get; set; }

    /// <summary>
    /// Initializes the saga state from the manager command and emits the first
    /// <see cref="RemoveMediaCommand"/> to delete the avatar.
    /// </summary>
    /// <param name="cmd">The manager-initiated command that started the saga.</param>
    /// <param name="logger">Logger used to record saga start.</param>
    /// <returns>The initialized saga and the first media removal command to dispatch.</returns>
    public static (DeleteDoctorSaga, RemoveMediaCommand) Start(
        DeleteDoctorByManagerCommand cmd,
        ILogger<DeleteDoctorSaga> logger)
    {
        var saga = new DeleteDoctorSaga
        {
            Id = Guid.NewGuid(),
            ManagerDoctorId = cmd.ManagerDoctorId,
            EntityId = cmd.EntityId,
            MediaId = cmd.MediaId,
        };

        logger.LogInformation("DeleteDoctor saga {SagaId} started by manager {ManagerId}", saga.Id, cmd.ManagerDoctorId);

        return (saga, new RemoveMediaCommand
        {
            SagaId = saga.Id,
            MediaId = saga.MediaId,
        });
    }

    /// <summary>
    /// Step 1 — avatar removed; emits the <see cref="DeleteDoctorCommand"/> to delete
    /// the doctor profile.
    /// </summary>
    /// <param name="success">Confirmation that the media was removed.</param>
    /// <param name="logger">Logger used to record step progress.</param>
    /// <returns>The command that deletes the doctor profile.</returns>
    public DeleteDoctorCommand Handle(RemoveMediaSuccess success, ILogger<DeleteDoctorSaga> logger)
    {
        logger.LogInformation("DeleteDoctor saga {SagaId}: media removed", Id);
        return new DeleteDoctorCommand
        {
            SagaId = Id,
            DoctorId = EntityId,
            DoctorCreatorId = ManagerDoctorId,
        };
    }

    /// <summary>
    /// Media step failed. Marks the saga completed; deletion is terminal, so no
    /// compensation is attempted.
    /// </summary>
    /// <param name="failure">Details of the media removal failure.</param>
    /// <param name="logger">Logger used to record the failure.</param>
    public void Handle(RemoveMediaFailure failure, ILogger<DeleteDoctorSaga> logger)
    {
        logger.LogWarning("DeleteDoctor saga {SagaId}: media step failed - {Message}", Id, failure.Message);
        MarkCompleted();
    }

    /// <summary>
    /// Step 2 — doctor profile removed; emits the <see cref="DeleteAuthDoctorCommand"/>
    /// to delete the auth account.
    /// </summary>
    /// <param name="success">Confirmation that the doctor profile was deleted.</param>
    /// <param name="logger">Logger used to record step progress.</param>
    /// <returns>The command that deletes the auth account.</returns>
    public DeleteAuthDoctorCommand Handle(DeleteDoctorSuccess success, ILogger<DeleteDoctorSaga> logger)
    {
        logger.LogInformation("DeleteDoctor saga {SagaId}: doctor removed", Id);
        return new DeleteAuthDoctorCommand
        {
            SagaId = Id,
            AuthUserId = EntityId,
        };
    }

    /// <summary>
    /// Doctor step failed. Marks the saga completed; deletion is terminal, so no
    /// compensation is attempted.
    /// </summary>
    /// <param name="failure">Details of the doctor deletion failure.</param>
    /// <param name="logger">Logger used to record the failure.</param>
    public void Handle(DeleteDoctorFailure failure, ILogger<DeleteDoctorSaga> logger)
    {
        logger.LogWarning("DeleteDoctor saga {SagaId}: doctor step failed - {Message}", Id, failure.Message);
        MarkCompleted();
    }

    /// <summary>
    /// Final step — auth account removed. Marks the saga completed.
    /// </summary>
    /// <param name="success">Confirmation that the auth account was deleted.</param>
    /// <param name="logger">Logger used to record completion.</param>
    public void Handle(DeleteAuthDoctorSuccess success, ILogger<DeleteDoctorSaga> logger)
    {
        logger.LogInformation("DeleteDoctor saga {SagaId}: completed (auth {AuthUserId} removed)", Id, success.AuthUserId);
        MarkCompleted();
    }

    /// <summary>
    /// Auth step failed. Marks the saga completed; deletion is terminal, so no
    /// compensation is attempted.
    /// </summary>
    /// <param name="failure">Details of the auth deletion failure.</param>
    /// <param name="logger">Logger used to record the failure.</param>
    public void Handle(DeleteAuthDoctorFailure failure, ILogger<DeleteDoctorSaga> logger)
    {
        logger.LogWarning("DeleteDoctor saga {SagaId}: auth step failed - {Message}", Id, failure.Message);
        MarkCompleted();
    }

    /// <summary>Wolverine fallback when no running saga matches the incoming message; logs a warning.</summary>
    public static void NotFound(RemoveMediaSuccess msg, ILogger<DeleteDoctorSaga> logger) =>
        logger.LogWarning("DeleteDoctor saga not found for {SagaId}", msg.SagaId);
    /// <summary>Wolverine fallback when no running saga matches the incoming message; logs a warning.</summary>
    public static void NotFound(RemoveMediaFailure msg, ILogger<DeleteDoctorSaga> logger) =>
        logger.LogWarning("DeleteDoctor saga not found for {SagaId}", msg.SagaId);
    /// <summary>Wolverine fallback when no running saga matches the incoming message; logs a warning.</summary>
    public static void NotFound(DeleteDoctorSuccess msg, ILogger<DeleteDoctorSaga> logger) =>
        logger.LogWarning("DeleteDoctor saga not found for {SagaId}", msg.SagaId);
    /// <summary>Wolverine fallback when no running saga matches the incoming message; logs a warning.</summary>
    public static void NotFound(DeleteDoctorFailure msg, ILogger<DeleteDoctorSaga> logger) =>
        logger.LogWarning("DeleteDoctor saga not found for {SagaId}", msg.SagaId);
    /// <summary>Wolverine fallback when no running saga matches the incoming message; logs a warning.</summary>
    public static void NotFound(DeleteAuthDoctorSuccess msg, ILogger<DeleteDoctorSaga> logger) =>
        logger.LogWarning("DeleteDoctor saga not found for {SagaId}", msg.SagaId);
    /// <summary>Wolverine fallback when no running saga matches the incoming message; logs a warning.</summary>
    public static void NotFound(DeleteAuthDoctorFailure msg, ILogger<DeleteDoctorSaga> logger) =>
        logger.LogWarning("DeleteDoctor saga not found for {SagaId}", msg.SagaId);
}
