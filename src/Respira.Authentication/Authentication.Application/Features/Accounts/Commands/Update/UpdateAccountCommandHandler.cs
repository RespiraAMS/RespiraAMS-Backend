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
            var oldAccount = account;
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
            }

            var cachePrefix = $"auth:accounts:{command.Email}";
            await cacheService.RemoveAsync(cachePrefix);

            account!.Email = command.Email;
            account.HashPassword = hashService.HashPassword(command.Password);
            account.Phone = command.Phone;
            account.Role = command.Role;
            account.Status = command.Status;
            account.UpdatedAt = DateTime.UtcNow;
            dbContext.Accounts.Update(account);
            await dbContext.SaveChangesAsync();
            await cacheService.SetAsync(cachePrefix, account);
            await messageBus.PublishAsync(
                new UpdateAccountCompleted()
                {
                    SagaId = command.SagaId,
                    OldEmail = oldAccount!.Email,
                    OldPassword = oldAccount.HashPassword,
                    OldPhone = oldAccount.Phone,
                    OldRole = oldAccount.Role,
                    OldStatus = oldAccount.Status,
                    AccountId = oldAccount.Id,
                }
            );
        }
    }
}
