using Application.Contracts.Data;
using Microsoft.EntityFrameworkCore;
using X.PagedList.EF;

namespace Application.Features.Pathogens.GetPagedPathogen;

public class GetPagedPathogensHandler(IDbContext context, IPaginationFactory factory)
    : IQueryHandler<GetPagedPathogenQuery, Pagination<PagedPathogenItem>>
{
    public async Task<Pagination<PagedPathogenItem>> HandleAsync(GetPagedPathogenQuery query,
        CancellationToken cancellationToken = default)
    {
        // Apply filter
        var queryable = context.Pathogens.AsQueryable();
        if (query.Filter is not null)
        {
            if (query.Filter.Name is not null)
            {
                queryable = queryable.Where(x =>
                    EF.Functions.ILike(x.Name, $"%{query.Filter.Name}%")
                );
            }
        }

        // Get paged pathogen
        var pathogens = await queryable
            .AsNoTracking()
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new PagedPathogenItem()
            {
                Id = x.Id,
                Name = x.Name,
                Description = x.Description,
            })
            .ToPagedListAsync(query.Param.Page, query.Param.Size);
        return factory.Create(pathogens);
    }
}