using Microsoft.Extensions.Logging;
using Respira.Clinical.Application.Contracts.Data;
using Respira.Clinical.Application.Contracts.Mappers;
using Respira.Clinical.Domain.Entities;
using Respira.ServiceDefaults.Contracts.CQRS;
using Respira.ServiceDefaults.Contracts.Results;
using Microsoft.EntityFrameworkCore;

namespace Respira.Clinical.Application.Features.RiskFactors.CreateRiskFactor
{
    public class CreateRiskFactorHandler(
        IDbContext context,
        ICreateMapper<CreateRiskFactorCommand, RiskFactor> mapper,
        ILogger<CreateRiskFactorHandler> logger)
        : ICommandHandler<CreateRiskFactorCommand, Result<CreateRiskFactorResult>>
    {
        public async Task<Result<CreateRiskFactorResult>> HandleAsync(CreateRiskFactorCommand command, CancellationToken cancellationToken = default)
        {
            // Check for unique: if any risk factor with (pathogen, criterion) exists
            if (await context.RiskFactors.AnyAsync(x => x.PathogenId == command.PathogenId && x.CriterionId == command.CriterionId, cancellationToken))
            {
                logger.LogDebug("The risk factor with this pathogen and criterion combination has already exist");
                return Result<CreateRiskFactorResult>.Failure(new Error(
                    ApplicationStatus.BadRequest,
                    "The risk factor with this pathogen and criterion combination has already exist"));
            }

            // Map from command to model
            var mapResult = mapper.ToModel(command);
            if (mapResult.IsFailure())
            {
                logger.LogDebug("Failed to map from command to risk factor model: {error}", mapResult.Error!);
                return Result<CreateRiskFactorResult>.Failure(mapResult.Error!);
            }
            var riskFactor = mapResult.Data!;

            // Save changes to database
            await context.RiskFactors.AddAsync(riskFactor, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
            return Result<CreateRiskFactorResult>.Success(ApplicationStatus.Created, new CreateRiskFactorResult(riskFactor.Id));
        }
    }
}
