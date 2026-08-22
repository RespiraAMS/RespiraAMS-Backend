using Application.Abstracts.Data;
using Application.Features.Authentication.UpdateUser.Rollback.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Respira.ServiceDefaults.Constracts.CQRS;
using Wolverine;

namespace Application.Features.Authentication.UpdateUser.Rollback.Commands;

public class RollbackUpdateAuthDoctorCommandHandler(
    ILogger<RollbackUpdateAuthDoctorCommand> logger,
    IAuthDbContext dbContext,
    IMessageBus bus
) : ICommandHandler<RollbackUpdateAuthDoctorCommand>
{
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
