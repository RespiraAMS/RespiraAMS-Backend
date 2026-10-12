using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Respira.Clinical.Application.Contracts.Data;
using Respira.ServiceDefaults.Contracts.CQRS;
using Respira.ServiceDefaults.Contracts.Results;

namespace Respira.Clinical.Application.Features.ClinicalMetrics.DeleteClinicalMetric
{
    public class DeleteClinicalMetricHandler(IDbContext context, ILogger<DeleteClinicalMetricHandler> logger)
        : ICommandHandler<DeleteClinicalMetricCommand, Result>
    {
        public async Task<Result> HandleAsync(DeleteClinicalMetricCommand command, CancellationToken cancellationToken = default)
        {
            // Get clinical metric from database
            var metric = await context.ClinicalMetrics
                .Include(x => x.Rules)
                .FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken);
            if (metric is null)
            {
                logger.LogDebug("Clinical metric with id {Id} not found", command.Id);
                return Result.Failure(new Error(ApplicationStatus.BadRequest, "Clinical metric not found"));
            }

            // Delete metrics and all its metric rules
            await context.ExecuteInTransactionAsync(async () =>
            {
                // Delete clinical metric
                metric.IsDeleted = true;
                metric.DeletedAt = DateTimeOffset.UtcNow;

                // Delete all metric rules
                foreach (var rule in metric.Rules)
                {
                    rule.IsDeleted = true;
                    rule.DeletedAt = DateTimeOffset.UtcNow;
                }

                logger.LogDebug("Clinical metric cascade deleted: {detail}", new
                {
                    MetricId = metric.Id,
                    RuleCascadeCount = metric.Rules.Count,
                });
            }, cancellationToken);

            return Result.Success(ApplicationStatus.Deleted);
        }
    }
}
