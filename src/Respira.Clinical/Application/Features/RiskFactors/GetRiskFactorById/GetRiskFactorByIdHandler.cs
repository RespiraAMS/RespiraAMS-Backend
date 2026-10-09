using Microsoft.EntityFrameworkCore;
using Respira.Clinical.Application.Contracts.Data;
using Respira.ServiceDefaults.Contracts.CQRS;
using Respira.ServiceDefaults.Contracts.Results;

namespace Respira.Clinical.Application.Features.RiskFactors.GetRiskFactorById
{
    public class GetRiskFactorByIdHandler(IDbContext context)
        : IQueryHandler<GetRiskFactorByIdQuery, Result<RiskFactorResult>>
    {
        public async Task<Result<RiskFactorResult>> HandleAsync(GetRiskFactorByIdQuery query, CancellationToken cancellationToken = default)
        {
            // Get risk factor by ID
            var riskFactor = await context.RiskFactors
                .AsNoTracking()
                .Select(x => new RiskFactorResult
                {
                    Id = x.Id,
                    Pathogen = new PathogenResult(x.Pathogen.Id, x.Pathogen.Name, x.Pathogen.IsAtypical),
                    Criterion = new CriterionResult(x.Criterion.Id, x.Criterion.Name, x.Criterion.Formula.ToString()!)
                })
                .FirstOrDefaultAsync(x => x.Id == query.Id, cancellationToken);
            return riskFactor is null ?
                Result<RiskFactorResult>.Failure(new Error(ApplicationStatus.ResourceNotFound, "Risk factor not found")) :
                Result<RiskFactorResult>.Success(ApplicationStatus.Success, riskFactor);
        }
    }
}
