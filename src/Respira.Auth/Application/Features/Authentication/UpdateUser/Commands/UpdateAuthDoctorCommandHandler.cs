using Application.Abstracts.Data;
using Application.Features.Authentication.UpdateUser.Events;
using Microsoft.Extensions.Logging;
using Respira.ServiceDefaults.Contracts.CQRS;
using Wolverine;

namespace Application.Features.Authentication.UpdateUser.Commands;

/// <summary>
/// Handles <see cref="UpdateAuthDoctorCommand"/>: applies the new email/phone/role to the
/// auth doctor account and emits <c>UpdateAuthDoctorSuccess</c> or <c>UpdateAuthDoctorFailure</c>
/// to drive the UpdateUser saga forward or compensate.
/// </summary>
public class UpdateAuthDoctorCommandHandler(
    ILogger<UpdateAuthDoctorCommand> logger,
    IAuthDbContext dbContext,
    IMessageBus bus
) : ICommandHandler<UpdateAuthDoctorCommand>
{
    /// <summary>
    /// Updates the auth doctor account and publishes the corresponding saga outcome event.
    /// </summary>
    /// <param name="command">Update command with the new and previous account details.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task HandleAsync(
        UpdateAuthDoctorCommand command,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            var authDoctor = await dbContext.AuthDoctors.FindAsync(command.AuthUserId);
            if (authDoctor is null)
            {
                await bus.PublishAsync(
                    new UpdateAuthDoctorFailure
                    {
                        SagaId = command.SagaId,
                        Message = "Auth user not found",
                    }
                );
                return;
            }

            authDoctor.Email = command.Email.ToLowerInvariant();
            authDoctor.Phone = command.Phone;
            authDoctor.Role = command.Role;

            await dbContext.SaveChangesAsync();
            await bus.PublishAsync(
                new UpdateAuthDoctorSuccess { SagaId = command.SagaId, AuthUserId = authDoctor.Id }
            );
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to update auth doctor");
            await bus.PublishAsync(
                new UpdateAuthDoctorFailure
                {
                    SagaId = command.SagaId,
                    Message = "Failed to update user account",
                }
            );
        }
    }
}
