using Authentication.Application.Constracts.Authentication;
using Authentication.Application.Constracts.Cache;
using Authentication.Application.Constracts.Data;
using Authentication.Application.Features.Accounts.Commands.Create.Events;
using Authentication.Domain.Entities;
using JasperFx.Events.Documents;
using Microsoft.Extensions.Logging;
using Respira.ServiceDefaults.Contracts.CQRS;
using Wolverine;

namespace Authentication.Application.Features.Accounts.Commands.Create
{
    public class CreateAccountCommandHandler(
        IMessageBus bus,
        IAuthDbContext dbContext,
        ILogger<CreateAccountCommandHandler> logger,
        ICacheService cacheService,
        IHashService hashService
    ) : ICommandHandler<CreateAccountCommand>
    {
        public async Task HandleAsync(
            CreateAccountCommand command,
            CancellationToken cancellationToken = default
        )
        {
            var isExist = await dbContext.Accounts.AnyAsync(x => x.Email == command.Email);

            if (isExist)
            {
                logger.LogDebug("{SagaId} Account is already exists", command.SagaId);

                await bus.PublishAsync(
                    new CreateAccountFailure(command.SagaId, "Account already exists")
                );
            }

            await dbContext.Accounts.AddAsync(
                new Account()
                {
                    Email = command.Email,
                    HashPassword = hashService.HashPassword(command.Password),
                    Phone = command.Phone,
                    Role = command.Role,
                    Status = command.Status,
                }
            );

            var cachePrefix = $"auth:accounts:{command.Email}";
            await cacheService.SetAsync(
                cachePrefix,
                new Account()
                {
                    Email = command.Email,
                    HashPassword = hashService.HashPassword(command.Password),
                    Phone = command.Phone,
                    Role = command.Role,
                    Status = command.Status,
                }
            );
        }
    }
}
