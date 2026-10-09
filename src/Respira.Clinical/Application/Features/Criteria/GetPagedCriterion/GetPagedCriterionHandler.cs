using Microsoft.EntityFrameworkCore;
using Respira.Clinical.Application.Contracts.Data;
using Respira.Clinical.Application.Contracts.Mappers;
using Respira.ServiceDefaults.Contracts.CQRS;
using Respira.ServiceDefaults.Contracts.Pagination;
using Respira.ServiceDefaults.Contracts.Results;
using X.PagedList.EF;

namespace Respira.Clinical.Application.Features.Criteria.GetPagedCriterion
{
    public class GetPagedCriterionHandler(IDbContext context, IPaginationFactory factory)
        : IQueryHandler<GetPagedCriterionQuery, Result<Pagination<PagedCriterionItem>>>
    {
        public async Task<Result<Pagination<PagedCriterionItem>>> HandleAsync(GetPagedCriterionQuery query, CancellationToken cancellationToken = default)
        {
            var criteria = await context.Criteria
                .AsNoTracking()
                .OrderByDescending(x => x.CreatedAt)
                .Select(x => new PagedCriterionItem(x.Id, x.Name, x.Formula.ToString()!))
                .ToPagedListAsync(query.Param.Page, query.Param.Size);

            return Result<Pagination<PagedCriterionItem>>.Success(ApplicationStatus.Success, factory.Create(criteria));
        }
    }
}
