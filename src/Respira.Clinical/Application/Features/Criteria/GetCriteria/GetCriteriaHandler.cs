using Microsoft.EntityFrameworkCore;
using Respira.Clinical.Application.Contracts.Data;
using Respira.ServiceDefaults.Contracts.CQRS;
using Respira.ServiceDefaults.Contracts.Results;

namespace Respira.Clinical.Application.Features.Criteria.GetCriteria
{
    public class GetCriteriaHandler(IDbContext context) : IQueryHandler<GetCriteriaQuery, Result<GetCriteriaResult>>
    {
        public async Task<Result<GetCriteriaResult>> HandleAsync(GetCriteriaQuery query, CancellationToken cancellationToken = default)
        {
            var criteria = await context.Criteria
                .AsNoTracking()
                .OrderBy(x => x.Name)
                .Select(x => new CriterionItem(x.Id, x.Name))
                .ToListAsync(cancellationToken);
            return Result<GetCriteriaResult>.Success(ApplicationStatus.Success, new GetCriteriaResult(criteria));
        }
    }
}
