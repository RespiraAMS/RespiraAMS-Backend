using Microsoft.EntityFrameworkCore;
using Respira.Clinical.Application.Contracts.Data;
using Respira.Clinical.Application.Contracts.Mappers;
using Respira.ServiceDefaults.Contracts.CQRS;
using Respira.ServiceDefaults.Contracts.Pagination;
using Respira.ServiceDefaults.Contracts.Results;
using X.PagedList.EF;

namespace Respira.Clinical.Application.Features.RiskFactors.GetPagedRiskFactor
{
    public class GetPagedRiskFactorHandler(IDbContext context, IPaginationFactory factory)
        : IQueryHandler<GetPagedRiskFactorQuery, Result<Pagination<PagedRiskFactorItem>>>
    {
        public async Task<Result<Pagination<PagedRiskFactorItem>>> HandleAsync(GetPagedRiskFactorQuery query, CancellationToken cancellationToken = default)
        {
            // Apply filter
            var queryable = context.RiskFactors.AsQueryable();
            if (query.Filter?.PathogenId is not null)
            {
                queryable = queryable.Where(x => x.PathogenId == query.Filter.PathogenId);
            }

            var factors = await queryable
                .AsNoTracking()
                .OrderByDescending(x => x.CreatedAt)
                .Select(x => new PagedRiskFactorItem
                {
                    Id = x.Id,
                    Pathogen = new PathogenResult(x.PathogenId, x.Pathogen.Name),
                    Criterion = new CriterionResult(x.CriterionId, x.Criterion.Name, x.Criterion.Formula.ToString()!)
                })
                .ToPagedListAsync(query.Param.Page, query.Param.Size);
            return Result<Pagination<PagedRiskFactorItem>>.Success(ApplicationStatus.Success, factory.Create(factors));
        }
    }
}
