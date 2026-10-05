using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Respira.Clinical.Application.Contracts.Data;
using Respira.ServiceDefaults.Contracts.CQRS;
using Respira.ServiceDefaults.Contracts.Results;

namespace Respira.Clinical.Application.Features.Pathogens.DeletePathogen
{
    public class DeletePathogenHandler(IDbContext context, ILogger<DeletePathogenHandler> logger)
        : ICommandHandler<DeletePathogenCommand, Result>
    {
        public async Task<Result> HandleAsync(DeletePathogenCommand command, CancellationToken cancellationToken = default)
        {
            // Get pathogen by ID
            var pathogen = await context.Pathogens
                .Include(x => x.RiskFactors)
                .FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken);
            if (pathogen is null)
            {
                logger.LogWarning("Pathogen ID not found");
                return Result.Failure(new Error(ApplicationStatus.BadRequest, "Pathogen ID not found"));
            }

            // Delete cascade in transaction
            await context.ExecuteInTransactionAsync(async () =>
            {
                // Delete pathogen
                pathogen.IsDeleted = true;
                pathogen.DeletedAt = DateTimeOffset.UtcNow;

                foreach (var risk in pathogen.RiskFactors)
                {
                    risk.IsDeleted = true;
                    risk.DeletedAt = DateTimeOffset.UtcNow;
                }

                // Cascade delete: SuspectedCause
                var suspectedCauseCount = await context.SuspectedCauses
                    .Where(x => x.PathogenId == pathogen.Id)
                    .ExecuteUpdateAsync(x => x
                        .SetProperty(sc => sc.IsDeleted, true)
                        .SetProperty(sc => sc.DeletedAt, DateTimeOffset.UtcNow), cancellationToken);

                // Log result
                logger.LogInformation("Cascade delete pathogen: {result}", new
                {
                    RiskFactorCount = pathogen.RiskFactors.Count,
                    SuspectedCauseCount = suspectedCauseCount
                });
            }, cancellationToken);

            return Result.Success(ApplicationStatus.Deleted);
        }
    }
}
