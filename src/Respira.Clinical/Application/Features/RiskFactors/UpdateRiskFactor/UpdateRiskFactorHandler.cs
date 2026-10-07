using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Respira.Clinical.Application.Contracts.Data;
using Respira.Clinical.Application.Contracts.Mappers;
using Respira.Clinical.Domain.Entities;
using Respira.ServiceDefaults.Contracts.CQRS;
using Respira.ServiceDefaults.Contracts.Results;

namespace Respira.Clinical.Application.Features.RiskFactors.UpdateRiskFactor
{
    public class UpdateRiskFactorHandler(
        IDbContext context,
        IUpdateMapper<RiskFactor, UpdateRiskFactorCommand> mapper,
        ILogger<UpdateRiskFactorHandler> logger)
        : ICommandHandler<UpdateRiskFactorCommand, Result>
    {
        public async Task<Result> HandleAsync(UpdateRiskFactorCommand command, CancellationToken cancellationToken = default)
        {
            // Get risk factor by ID
            var factor = await context.RiskFactors.FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken);
            if (factor is null)
            {
                logger.LogDebug("Risk factor not found: {Id}", command.Id);
                return Result.Failure(new Error(ApplicationStatus.BadRequest, "Risk factor not found"));
            }

            // Check if the new update violate the unique constraints on risk factor
            var violateUnique = await context.RiskFactors
                .AnyAsync(x =>
                        x.Id != command.Id &&
                        x.PathogenId == command.PathogenId &&
                        x.CriterionId == command.CriterionId, cancellationToken);
            if (violateUnique)
            {
                const string msg = "Update violate unique constraints on risk factor: pathogen and criterion combination must be unique";
                logger.LogDebug(msg);
                return Result.Failure(new Error(ApplicationStatus.BadRequest, msg));
            }

            // Map from command to model
            var mapResult = mapper.MapModel(factor, command);
            if (mapResult.IsFailure())
            {
                logger.LogDebug("Failed to map from command to model: {error}", mapResult.Error!);
                return Result.Failure(mapResult.Error!);
            }

            // Save changes to database
            await context.SaveChangesAsync(cancellationToken);
            return Result.Success(ApplicationStatus.Updated);
        }
    }
}
