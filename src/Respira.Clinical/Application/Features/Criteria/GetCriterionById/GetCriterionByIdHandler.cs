using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Respira.Clinical.Application.Contracts.Data;
using Respira.Clinical.Application.Contracts.Mappers;
using Respira.Clinical.Application.Features.Shared.ManageFormula;
using Respira.Clinical.Domain.Models;
using Respira.ServiceDefaults.Contracts.CQRS;
using Respira.ServiceDefaults.Contracts.Results;

namespace Respira.Clinical.Application.Features.Criteria.GetCriterionById
{
    public class GetCriterionByIdHandler(
        IDbContext context,
        IMapper<Formula, FormulaDto> mapper,
        ILogger<GetCriterionByIdHandler> logger)
        : IQueryHandler<GetCriterionByIdQuery, Result<CriterionResult>>
    {
        public async Task<Result<CriterionResult>> HandleAsync(GetCriterionByIdQuery query, CancellationToken cancellationToken = default)
        {
            var criterion = await context.Criteria
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == query.Id, cancellationToken);
            if (criterion is null)
            {
                logger.LogDebug("Criterion with ID {id} does not exist", query.Id);
                return Result<CriterionResult>.Failure(new Error(ApplicationStatus.ResourceNotFound, "Criterion does not exist"));
            }

            var mapResult = mapper.Map(criterion.Formula);
            if (mapResult.IsFailure())
            {
                logger.LogDebug("Failed to map criterion formula: {error}", mapResult.Error!);
                return Result<CriterionResult>.Failure(mapResult.Error!);
            }

            return Result<CriterionResult>.Success(ApplicationStatus.Success, new CriterionResult
            {
                Id = criterion.Id,
                Name = criterion.Name,
                FormulaString = criterion.Formula.ToString()!,
                Formula = mapResult.Data!
            });
        }
    }
}
