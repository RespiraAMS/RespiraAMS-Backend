using Authentication.Application.Constracts.Authentication;
using Authentication.Application.Constracts.Cache;
using Authentication.Application.Constracts.Data;
using Authentication.Application.Features.Accounts.Commands.Update.Events;
using Microsoft.Extensions.Logging;
using Respira.ServiceDefaults.Contracts.CQRS;
using Wolverine;

namespace Authentication.Application.Features.Accounts.Commands.Update
{
    public class UpdateAccountCommandHandler(
        IAuthDbContext dbContext,
        ILogger<UpdateAccountCommandHandler> logger,
        ICacheService cacheService,
        IHashService hashService,
        IMessageBus messageBus
    ) : ICommandHandler<UpdateAccountCommand>
    {
        public async Task HandleAsync(
            UpdateAccountCommand command,
            CancellationToken cancellationToken = default
        )
        {
            var account = await dbContext.Accounts.FindAsync(command.AccountId);

            if (account is null)
            {
                logger.LogDebug("{AccountId} not found", command.AccountId);
                await messageBus.PublishAsync(
                    new UpdateAccountFailure()
                    {
                        AccountId = command.AccountId,
                        SagaId = command.SagaId,
                        Reason = "Account not found",
                    }
                );
                return;
            }

            // Captured before the entity is mutated: the event reports the previous values.
            var accountId = account.Id;
            var oldEmail = account.Email;
            var oldHash = account.HashPassword;
            var oldPhone = account.Phone;
            var oldRole = account.Role;
            var oldStatus = account.Status;

            try
            {
                var cachePrefix = $"auth:accounts:{command.Email}";
                await cacheService.RemoveAsync(cachePrefix, cancellationToken);

                // When the address changes the entry cached under the previous one must go
                // too, otherwise lookups keep resolving to the stale account.
                var oldCachePrefix = $"auth:accounts:{oldEmail}";
                if (!string.Equals(oldCachePrefix, cachePrefix, StringComparison.Ordinal))
                {
                    await cacheService.RemoveAsync(oldCachePrefix, cancellationToken);
                }

                account.Email = command.Email;
                account.Phone = command.Phone;

                // Optional fields: a caller that leaves them null keeps the current value.
                if (!string.IsNullOrEmpty(command.Password))
                {
                    account.HashPassword = hashService.HashPassword(command.Password);
                }

                if (command.Role.HasValue)
                {
                    account.Role = command.Role.Value;
                }

                if (command.Status.HasValue)
                {
                    account.Status = command.Status.Value;
                }

                account.UpdatedAt = DateTime.UtcNow;
                dbContext.Accounts.Update(account);
                await dbContext.SaveChangesAsync(cancellationToken);
                await cacheService.SetAsync(cachePrefix, account, cancellationToken: cancellationToken);
            }
            catch (Exception exception)
            {
                // The saga waits for an answer either way: report the failure instead of
                // letting Wolverine retry a handler the saga cannot observe.
                logger.LogError(exception, "{AccountId} failed to update", command.AccountId);
                await messageBus.PublishAsync(
                    new UpdateAccountFailure()
                    {
                        AccountId = command.AccountId,
                        SagaId = command.SagaId,
                        Reason = exception.Message,
                    }
                );
                return;
            }

            await messageBus.PublishAsync(
                new UpdateAccountCompleted()
                {
                    SagaId = command.SagaId,
                    OldEmail = oldEmail,
                    OldPassword = oldHash,
                    OldPhone = oldPhone,
                    OldRole = oldRole,
                    OldStatus = oldStatus,
                    AccountId = accountId,
                }
            );
        }
    }
}
