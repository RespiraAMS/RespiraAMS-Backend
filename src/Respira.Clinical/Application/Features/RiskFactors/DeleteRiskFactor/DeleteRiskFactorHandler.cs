using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Respira.Clinical.Application.Contracts.Data;
using Respira.ServiceDefaults.Contracts.CQRS;
using Respira.ServiceDefaults.Contracts.Results;

namespace Respira.Clinical.Application.Features.RiskFactors.DeleteRiskFactor
{
    public class DeleteRiskFactorHandler(IDbContext context, ILogger<DeleteRiskFactorHandler> logger)
        : ICommandHandler<DeleteRiskFactorCommand, Result>
    {
        public async Task<Result> HandleAsync(DeleteRiskFactorCommand command, CancellationToken cancellationToken = default)
        {
            // Get risk factor by ID
            var factor = await context.RiskFactors.FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken);
            if (factor is null)
            {
                logger.LogDebug("Risk factor not found: {Id}", command.Id);
                return Result.Failure(new Error(ApplicationStatus.BadRequest, "Risk factor not found"));
            }

            // Delete risk factor
            factor.IsDeleted = true;
            factor.DeletedAt = DateTimeOffset.UtcNow;
            await context.SaveChangesAsync(cancellationToken);
            return Result.Success(ApplicationStatus.Deleted);
        }
    }
}
