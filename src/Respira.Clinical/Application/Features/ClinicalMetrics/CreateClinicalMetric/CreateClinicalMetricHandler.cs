using Microsoft.Extensions.Logging;
using Respira.Clinical.Application.Contracts.Data;
using Respira.Clinical.Application.Contracts.Mappers;
using Respira.Clinical.Domain.Entities;
using Respira.ServiceDefaults.Contracts.CQRS;
using Respira.ServiceDefaults.Contracts.Results;
using Microsoft.EntityFrameworkCore;

namespace Respira.Clinical.Application.Features.ClinicalMetrics.CreateClinicalMetric
{
    public class CreateClinicalMetricHandler(
        IDbContext context,
        ICreateMapper<CreateClinicalMetricCommand, ClinicalMetric> mapper,
        ILogger<CreateClinicalMetricHandler> logger)
        : ICommandHandler<CreateClinicalMetricCommand, Result<CreateClinicalMetricResult>>
    {
        public async Task<Result<CreateClinicalMetricResult>> HandleAsync(CreateClinicalMetricCommand command, CancellationToken cancellationToken = default)
        {
            // Check if code is unique
            var existingMetric = await context.ClinicalMetrics
                .AnyAsync(x => x.Code.Equals(command.Code) && x.IsApplied, cancellationToken);
            if (existingMetric)
            {
                logger.LogDebug("Clinical metric with code {Code} already exists and in used", command.Code);
                return Result<CreateClinicalMetricResult>.Failure(new Error(ApplicationStatus.BadRequest, "Clinical metric already exists and in used"));
            }

            // Creating a new metric is always applied

            // Map from command to domain model
            var mapResult = mapper.ToModel(command);
            if (mapResult.IsFailure())
            {
                logger.LogDebug("Failed to map command to domain model: {error}", mapResult.Error!);
                return Result<CreateClinicalMetricResult>.Failure(mapResult.Error!);
            }
            var metric = mapResult.Data!;

            // Save changes to database
            await context.ClinicalMetrics.AddAsync(metric, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
            return Result<CreateClinicalMetricResult>.Success(ApplicationStatus.Created, new CreateClinicalMetricResult(metric.Id));
        }
    }
}
