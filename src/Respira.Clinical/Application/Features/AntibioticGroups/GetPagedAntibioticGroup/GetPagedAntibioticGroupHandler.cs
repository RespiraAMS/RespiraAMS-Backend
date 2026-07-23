using Application.Contracts.Data;
using Microsoft.EntityFrameworkCore;
using X.PagedList.EF;

namespace Application.Features.AntibioticGroups.GetPagedAntibioticGroup;

public class GetPagedAntibioticGroupHandler(IDbContext context, IPaginationFactory factory)
    : IQueryHandler<GetPagedAntibioticGroupQuery, Pagination<GetPagedAntibioticGroupResult>>
{
    public async Task<Pagination<GetPagedAntibioticGroupResult>> HandleAsync(GetPagedAntibioticGroupQuery query,
        CancellationToken cancellationToken = default)
    {
        // Apply filter
        var queryable = context.AntibioticGroups.AsQueryable();
        if (query.Filter is not null)
        {
            // Search name (contains, case-insensitive)
            if (query.Filter.Name is not null)
            {
                queryable = queryable.Where(x =>
                    EF.Functions.ILike(x.Name, $"%{query.Filter.Name}%"));
            }

            // Filter by parent ID
            if (query.Filter.ParentId is not null)
            {
                queryable = queryable.Where(x => x.ParentId == query.Filter.ParentId);
            }
        }

        // Get paged result
        var groups = await queryable
            .Select(x => new GetPagedAntibioticGroupResult
            {
                Id = x.Id,
                Name = x.Name,
                ParentId = x.ParentId,
                ParentName = x.Parent == null ? null : x.Parent.Name,
            })
            .ToPagedListAsync(query.Param.Page, query.Param.Size);

        return factory.Create(groups);
    }
}