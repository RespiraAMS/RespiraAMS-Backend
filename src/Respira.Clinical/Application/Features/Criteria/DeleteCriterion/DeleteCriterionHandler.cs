using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Respira.Clinical.Application.Contracts.Data;
using Respira.ServiceDefaults.Contracts.CQRS;
using Respira.ServiceDefaults.Contracts.Results;

namespace Respira.Clinical.Application.Features.Criteria.DeleteCriterion
{
    public class DeleteCriterionHandler(IDbContext context, ILogger<DeleteCriterionHandler> logger)
        : ICommandHandler<DeleteCriterionCommand, Result>
    {
        public async Task<Result> HandleAsync(DeleteCriterionCommand command, CancellationToken cancellationToken = default)
        {
            // Get the criterion in database
            var criterion = await context.Criteria.FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken);
            if (criterion is null)
            {
                logger.LogDebug("Criterion with this ID not found: {Id}", command.Id);
                return Result.Failure(new Error(ApplicationStatus.BadRequest, "Criterion not found"));
            }

            // Delete criterion and all entities that associated with it
            await context.ExecuteInTransactionAsync(async () =>
            {
                // Soft delete criterion
                criterion.IsDeleted = true;
                criterion.DeletedAt = DateTimeOffset.UtcNow;

                // Delete all risk factors that used this criterion
                var factorCount = await context.RiskFactors
                    .Where(x => x.CriterionId == command.Id)
                    .ExecuteUpdateAsync(x => x
                        .SetProperty(rf => rf.IsDeleted, true)
                        .SetProperty(rf => rf.DeletedAt, DateTimeOffset.UtcNow), cancellationToken);

                // Delete all clinical metrics rule that used this criterion
                var ruleCount = await context.MetricsRules
                    .Where(x => x.CriterionId == command.Id)
                    .ExecuteUpdateAsync(x => x
                        .SetProperty(mr => mr.IsDeleted, true)
                        .SetProperty(mr => mr.DeletedAt, DateTimeOffset.UtcNow), cancellationToken);

                // Delete all treatments that used this criterion
                var treatmentCount = await context.Treatments
                    .Where(x => x.Criteria.Select(x => x.Id).Contains(command.Id))
                    .ExecuteUpdateAsync(x => x
                        .SetProperty(t => t.IsDeleted, true)
                        .SetProperty(t => t.DeletedAt, DateTimeOffset.UtcNow), cancellationToken);

                logger.LogDebug("Cascade delete criterion success: {detail}", new
                {
                    factorCount,
                    ruleCount,
                    treatmentCount
                });
            }, cancellationToken);

            return Result.Success(ApplicationStatus.Deleted);
        }
    }
}
