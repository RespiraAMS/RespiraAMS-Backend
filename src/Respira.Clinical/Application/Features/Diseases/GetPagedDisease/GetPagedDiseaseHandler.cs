using Application.Contracts.Data;
using Microsoft.EntityFrameworkCore;
using X.PagedList.EF;

namespace Application.Features.Diseases.GetPagedDisease;

public class GetPagedDiseaseHandler(IDbContext context, IPaginationFactory factory)
    : IQueryHandler<GetPagedDiseaseQuery, Pagination<PagedDiseaseItem>>
{
    public async Task<Pagination<PagedDiseaseItem>> HandleAsync(GetPagedDiseaseQuery query,
        CancellationToken cancellationToken = default)
    {
        // Apply filter
        var queryable = context.Diseases.AsQueryable();
        if (query.Filter?.Name is not null)
        {
            queryable = queryable
                .Where(x => EF.Functions.ILike(x.Name, $"%{query.Filter.Name}%"));
        }

        // Get paged disease
        var diseases = await queryable
            .AsNoTracking()
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new PagedDiseaseItem
            {
                Id = x.Id,
                Name = x.Name,
            })
            .ToPagedListAsync(query.Param.Page, query.Param.Size);
        return factory.Create(diseases);
    }
}