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
                return Result.Failure(new Error(ApplicationStatus.BadRequest, "Clinical metric not found"));
            }

            // Check if code is unique
            var existingMetric = await context.ClinicalMetrics
                .AnyAsync(x => x.Id != metric.Id && x.Code.Equals(command.Code) && x.IsApplied, cancellationToken);
            if (existingMetric)
            {
                logger.LogDebug("Clinical metric with code {Code} already exists and in used", command.Code);
                return Result.Failure(new Error(ApplicationStatus.BadRequest, "Clinical metric already exists and in used"));
            }

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
