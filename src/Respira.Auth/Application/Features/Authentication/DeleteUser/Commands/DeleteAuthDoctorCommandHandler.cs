using Application.Abstracts.Data;
using Application.Features.Authentication.DeleteUser.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Respira.ServiceDefaults.Constracts.CQRS;
using Wolverine;

namespace Application.Features.Authentication.DeleteUser.Commands;

public class DeleteAuthDoctorCommandHandler(
    ILogger<DeleteAuthDoctorCommand> logger,
    IAuthDbContext dbContext,
    IMessageBus bus
) : ICommandHandler<DeleteAuthDoctorCommand>
{
    public async Task HandleAsync(
        DeleteAuthDoctorCommand command,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            var authDoctor = await dbContext.AuthDoctors.FindAsync(command.AuthUserId);
            if (authDoctor is null)
            {
                await bus.PublishAsync(
                    new DeleteAuthDoctorSuccess
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
                new DeleteAuthDoctorSuccess
                {
                    SagaId = command.SagaId,
                    AuthUserId = command.AuthUserId,
                }
            );
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to delete auth doctor");
            await bus.PublishAsync(
                new DeleteAuthDoctorFailure
                {
                    SagaId = command.SagaId,
                    AuthUserId = command.AuthUserId,
                    Message = "Failed to delete user account",
                }
            );
        }
    }
}
