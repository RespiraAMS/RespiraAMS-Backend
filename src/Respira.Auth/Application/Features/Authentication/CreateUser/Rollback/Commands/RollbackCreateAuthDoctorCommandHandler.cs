using Application.Abstracts.Data;
using Application.Features.Authentication.CreateUser.Rollback.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Respira.ServiceDefaults.Constracts.CQRS;
using Wolverine;

namespace Application.Features.Authentication.CreateUser.Rollback.Commands;

public class RollbackCreateAuthDoctorCommandHandler(
    ILogger<RollbackCreateAuthDoctorCommand> logger,
    IAuthDbContext dbContext,
    IMessageBus bus
) : ICommandHandler<RollbackCreateAuthDoctorCommand>
{
    public async Task HandleAsync(
        RollbackCreateAuthDoctorCommand command,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            var authDoctor = await dbContext.AuthDoctors.FindAsync(command.AuthUserId);
            if (authDoctor is null)
            {
                logger.LogWarning(
                    "Auth doctor {AuthUserId} not found for rollback",
                    command.AuthUserId
                );
                await bus.PublishAsync(
                    new RollbackCreateAuthDoctorSuccess
                    {
                        SagaId = command.SagaId,
                        AuthUserId = command.AuthUserId,
                    }
                );
                return;
            }

            dbContext.AuthDoctors.Remove(authDoctor);
            await dbContext.SaveChangesAsync();

            await bus.PublishAsync(
                new RollbackCreateAuthDoctorSuccess
                {
                    SagaId = command.SagaId,
                    AuthUserId = command.AuthUserId,
                }
            );
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to rollback auth doctor");
            await bus.PublishAsync(
                new RollbackCreateAuthDoctorFailure
                {
                    SagaId = command.SagaId,
                    AuthUserId = command.AuthUserId,
                }
            );
        }
    }
}
