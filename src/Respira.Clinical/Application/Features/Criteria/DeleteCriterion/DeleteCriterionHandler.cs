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
                return Result.Failure(new ApplicationError(ApplicationStatus.BadRequest, "Criterion not found"));
            }

            // Business rule: if this criterion is being used somewhere, forbid delete
            // to keep data integrity
            if (await context.RiskFactors.AnyAsync(x => x.CriterionId == command.Id, cancellationToken))
            {
                return Result.Failure(new ApplicationError(
                    ApplicationStatus.BusinessRuleViolation,
                    "This criterion is being referenced in other risk factors, cannot delete"));
            }

            if (await context.MetricRules.AnyAsync(x => x.CriterionId == command.Id, cancellationToken))
            {
                return Result.Failure(new ApplicationError(
                    ApplicationStatus.BusinessRuleViolation,
                    "This criterion is being referenced in other metric rules, cannot delete"));
            }

            if (await context.TreatmentProtocols.AnyAsync(x => x.Criteria.Select(c => c.Id).Contains(command.Id), cancellationToken))
            {
                return Result.Failure(new ApplicationError(
                    ApplicationStatus.BusinessRuleViolation,
                    "This criterion is being referenced in other treatment protocols, cannot delete"));
            }

            // Soft delete criterion
            criterion.IsDeleted = true;
            criterion.DeletedAt = DateTimeOffset.UtcNow;
            await context.SaveChangesAsync(cancellationToken);
            return Result.Success(ApplicationStatus.Deleted);
        }
    }
}
