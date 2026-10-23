using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Respira.Clinical.Application.Contracts.Data;
using Respira.Clinical.Application.Contracts.Mappers;
using Respira.Clinical.Domain.Entities;
using Respira.ServiceDefaults.Contracts.CQRS;
using Respira.ServiceDefaults.Contracts.Results;

namespace Respira.Clinical.Application.Features.ClinicalMetrics.UpdateClinicalMetric
{
    public class UpdateClinicalMetricHandler(
        IDbContext context,
        IUpdateMapper<ClinicalMetric, UpdateClinicalMetricCommand> mapper,
        ILogger<UpdateClinicalMetricHandler> logger)
        : ICommandHandler<UpdateClinicalMetricCommand, Result>
    {
        public async Task<Result> HandleAsync(UpdateClinicalMetricCommand command, CancellationToken cancellationToken = default)
        {
            // Get clinical metric from database
            var metric = await context.ClinicalMetrics.FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken);
            if (metric is null)
            {
                logger.LogDebug("Clinical metric with id {Id} not found", command.Id);
                return Result.Failure(new ApplicationError(ApplicationStatus.BadRequest, "Clinical metric not found"));
            }

            // Business rule: code is immutable after creation, since it's is used for diagnosis

            // Map from command to domain model
            var mapResult = mapper.MapModel(metric, command);
            if (mapResult.IsFailure())
            {
                logger.LogDebug("Failed to map command to domain model: {error}", mapResult.Error!);
                return Result.Failure(mapResult.Error!);
            }

            // Save changes to database
            await context.SaveChangesAsync(cancellationToken);
            return Result.Success(ApplicationStatus.Updated);
        }
    }
}
