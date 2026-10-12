using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Respira.Clinical.Application.Contracts.Data;
using Respira.ServiceDefaults.Contracts.CQRS;
using Respira.ServiceDefaults.Contracts.Results;

namespace Respira.Clinical.Application.Features.ClinicalMetrics.DeleteRule
{
    public class DeleteRuleHandler(IDbContext context, ILogger<DeleteRuleHandler> logger)
        : ICommandHandler<DeleteRuleCommand, Result>
    {
        public async Task<Result> HandleAsync(DeleteRuleCommand command, CancellationToken cancellationToken = default)
        {
            // Get metric rule by ID
            var rule = await context.MetricRules.FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken);
            if (rule is null)
            {
                logger.LogDebug("Metric rule does not exist: {Id}", command.Id);
                return Result.Failure(new Error(ApplicationStatus.BadRequest, "Metric rule does not exist"));
            }

            // Delete the rule
            rule.IsDeleted = true;
            rule.DeletedAt = DateTimeOffset.UtcNow;
            await context.SaveChangesAsync(cancellationToken);
            return Result.Success(ApplicationStatus.Deleted);
        }
    }
}
