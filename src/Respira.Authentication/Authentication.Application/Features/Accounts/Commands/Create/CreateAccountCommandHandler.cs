using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Respira.Authentication.Application.Constracts.Authentication;
using Respira.Authentication.Application.Constracts.Cache;
using Respira.Authentication.Application.Constracts.Data;
using Respira.Authentication.Application.Features.Accounts.Commands.Create.Events;
using Respira.Authentication.Domain.Entities;
using Respira.ServiceDefaults.Contracts.CQRS;
using Wolverine;

namespace Respira.Authentication.Application.Features.Accounts.Commands.Create
{
    /// <summary>
    /// Creates the account for the create saga. The handler is idempotent from the
    /// saga's point of view: every outcome (already exists, persistence failure) is
    /// reported as an event so the saga can decide, and the handler never throws for a
    /// business rule violation.
    /// </summary>
    public class CreateAccountCommandHandler(
        IMessageBus bus,
        IAuthDbContext dbContext,
        ILogger<CreateAccountCommandHandler> logger,
        ICacheService cacheService,
        IHashService hashService
    ) : ICommandHandler<CreateAccountCommand>
    {
        /// <summary>Same cache entry shape as login/refresh/SendVerify (account by email).</summary>
        private static readonly TimeSpan AccountCacheExpiration = TimeSpan.FromMinutes(5);

        public async Task HandleAsync(
            CreateAccountCommand command,
            CancellationToken cancellationToken = default
        )
        {
            ArgumentNullException.ThrowIfNull(command);

            // The verification email lookup (and login) compare against the lowercased
            // address, so the account must be stored the same way.
            var email = command.Email.Trim().ToLowerInvariant();

            var isExist = await dbContext
                .Accounts.IgnoreQueryFilters()
                .AnyAsync(x => x.Email == email, cancellationToken);

            if (isExist)
            {
                logger.LogInformation(
                    "{SagaId} Account {Email} already exists",
                    command.SagaId,
                    email
                );

                await bus.PublishAsync(
                    new CreateAccountFailure(command.SagaId, "Account already exists")
                );
                return;
            }

            var account = new Account
            {
                Email = email,
                HashPassword = hashService.HashPassword(command.Password),
                Phone = command.Phone,
                Role = command.Role,
                Status = command.Status,
            };

            dbContext.Accounts.Add(account);

            try
            {
                await dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException exception)
            {
                // Unique index on Email lost a race against a concurrent registration.
                logger.LogWarning(
                    exception,
                    "{SagaId} Persisting account {Email} conflicted with an existing row",
                    command.SagaId,
                    email
                );

                await bus.PublishAsync(
                    new CreateAccountFailure(command.SagaId, "Account already exists")
                );
                return;
            }

            await cacheService.SetAsync(
                $"auth:account:email:{email}",
                account,
                AccountCacheExpiration,
                cancellationToken
            );

            logger.LogInformation("{SagaId} Account {AccountId} created", command.SagaId, account.Id);

            await bus.PublishAsync(
                new CreateAccountCompleted { SagaId = command.SagaId, AccountId = account.Id }
            );
        }
    }
}
