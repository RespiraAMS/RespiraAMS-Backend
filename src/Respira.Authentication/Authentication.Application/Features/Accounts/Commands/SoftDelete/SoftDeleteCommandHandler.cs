using Authentication.Application.Constracts.Cache;
using Authentication.Application.Constracts.Data;
using Authentication.Application.Features.Accounts.Commands.SoftDelete.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Respira.ServiceDefaults.Contracts.CQRS;
using Wolverine;

namespace Authentication.Application.Features.Accounts.Commands.SoftDelete
{
    /// <summary>
    /// Compensation step for the create saga: hides the account created by a flow that
    /// failed further down the line. Idempotent, so a retried compensation still succeeds.
    /// </summary>
    public class SoftDeleteCommandHandler(
        IAuthDbContext dbContext,
        ICacheService cacheService,
        ILogger<SoftDeleteCommandHandler> logger,
        IMessageBus bus
    ) : ICommandHandler<SoftDeleteCommand>
    {
        public async Task HandleAsync(
            SoftDeleteCommand command,
            CancellationToken cancellationToken = default
        )
        {
            ArgumentNullException.ThrowIfNull(command);

            try
            {
                var account = await dbContext
                    .Accounts.IgnoreQueryFilters()
                    .FirstOrDefaultAsync(x => x.Id == command.AccountId, cancellationToken);

                // Already gone (or never created) is the desired end state of a compensation.
                if (account is null || account.IsDeleted)
                {
                    logger.LogInformation(
                        "{SagaId} Account {AccountId} already absent or deleted",
                        command.SagaId,
                        command.AccountId
                    );

                    await bus.PublishAsync(
                        new SoftDeleteCompleted(command.SagaId, command.AccountId)
                    );
                    return;
                }

                account.IsDeleted = true;
                account.DeletedAt = DateTimeOffset.UtcNow;
                account.UpdatedAt = DateTimeOffset.UtcNow;

                await dbContext.SaveChangesAsync(cancellationToken);

                await cacheService.RemoveAsync(
                    $"auth:account:email:{account.Email.ToLowerInvariant()}",
                    cancellationToken
                );
                await cacheService.RemoveAsync(
                    $"auth:account:id:{account.Id}",
                    cancellationToken
                );

                logger.LogInformation(
                    "{SagaId} Account {AccountId} soft-deleted as compensation",
                    command.SagaId,
                    account.Id
                );

                await bus.PublishAsync(
                    new SoftDeleteCompleted(command.SagaId, command.AccountId)
                );
            }
            catch (Exception exception)
            {
                logger.LogError(
                    exception,
                    "{SagaId} Soft-deleting account {AccountId} failed",
                    command.SagaId,
                    command.AccountId
                );

                await bus.PublishAsync(
                    new SoftDeleteFailure(command.SagaId, exception.Message)
                );
            }
        }
    }
}
