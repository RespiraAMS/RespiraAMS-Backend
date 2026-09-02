using Application.Abstracts.Data;
using Domain.Entities;
using Microsoft.Extensions.Logging;
using Respira.ServiceDefaults.Contracts.CQRS;
using Respira.ServiceDefaults.Dtos;
using Respira.ServiceDefaults.Exceptions;

namespace Application.Features.CreateAudit
{
    public class CreateAuditCommandHandler(
        ISagaAuditDbContext dbContext,
        ILogger<CreateAuditCommandHandler> logger
    ) : ICommandHandler<CreateAuditCommand, Result<bool>>
    {
        public async Task<Result<bool>> HandleAsync(
            CreateAuditCommand command,
            CancellationToken cancellationToken = default
        )
        {
            try
            {
                var newAudit = new Audit()
                {
                    ActionType = command.ActionType,
                    Data = command.Data,
                    EntityName = command.EntityName,
                    ServiceName = command.ServiceName,
                    UserCreatedId = command.UserCreatedId,
                };
                await dbContext.Audits.AddAsync(newAudit);
                await dbContext.SaveChangesAsync(cancellationToken);
                return Result<bool>.Ok(true);
            }
            catch (Exception e)
            {
                logger.LogError(e, " create audit failed.");
                throw new ServerException();
            }
        }
    }
}
