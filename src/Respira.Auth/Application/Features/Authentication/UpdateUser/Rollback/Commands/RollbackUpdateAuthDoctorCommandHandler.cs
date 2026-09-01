using Application.Abstracts.Data;
using Application.Features.Authentication.UpdateUser.Rollback.Events;
using Microsoft.Extensions.Logging;
using Respira.ServiceDefaults.Contracts.CQRS;
using Wolverine;

namespace Application.Features.Authentication.UpdateUser.Rollback.Commands;

/// <summary>
/// Handles <see cref="RollbackUpdateAuthDoctorCommand"/>: reverts the auth doctor account to its
/// previous email/phone/role and emits <c>RollbackUpdateAuthDoctorSuccess</c> or
/// <c>RollbackUpdateAuthDoctorFailure</c> to complete the UpdateUser saga compensation.
/// </summary>
public class RollbackUpdateAuthDoctorCommandHandler(
    ILogger<RollbackUpdateAuthDoctorCommand> logger,
    IAuthDbContext dbContext,
    IMessageBus bus
) : ICommandHandler<RollbackUpdateAuthDoctorCommand>
{
    /// <summary>
    /// Restores the previous account values and publishes the rollback outcome.
    /// </summary>
    /// <param name="command">Rollback command holding the original account values.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task HandleAsync(
        RollbackUpdateAuthDoctorCommand command,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            var authDoctor = await dbContext.AuthDoctors.FindAsync(command.AuthUserId);
            if (authDoctor is null)
            {
                await bus.PublishAsync(
                    new RollbackUpdateAuthDoctorSuccess
                    {
                        SagaId = command.SagaId,
                        AuthUserId = command.AuthUserId,
                    }
                );
                return;
            }

            authDoctor.Email = command.OldEmail.ToLowerInvariant();
            authDoctor.Phone = command.OldPhone;
            authDoctor.Role = command.OldRole;

            await dbContext.SaveChangesAsync();
            await bus.PublishAsync(
                new RollbackUpdateAuthDoctorSuccess
                {
                    SagaId = command.SagaId,
                    AuthUserId = command.AuthUserId,
                }
            );
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to rollback auth doctor update");
            await bus.PublishAsync(
                new RollbackUpdateAuthDoctorFailure
                {
                    SagaId = command.SagaId,
                    AuthUserId = command.AuthUserId,
                }
            );
        }
    }
}
