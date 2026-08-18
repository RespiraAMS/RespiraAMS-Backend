using Application.Contracts.Data;
using Microsoft.EntityFrameworkCore;
using X.PagedList.EF;

namespace Application.Features.Antibiotics.GetPagedAntibiotic;

public class GetPagedAntibioticHandler(IDbContext context, IPaginationFactory factory)
    : IQueryHandler<GetPagedAntibioticQuery, Pagination<PagedAntibioticItem>>
{
    public async Task<Pagination<PagedAntibioticItem>> HandleAsync(GetPagedAntibioticQuery query,
        CancellationToken cancellationToken = default)
    {
        // Apply filter
        var queryable = context.Antibiotics.AsQueryable();
        if (query.Filter is not null)
        {
            if (query.Filter.Name is not null)
            {
                queryable = queryable
                    .Where(x => EF.Functions.ILike(x.Name, $"%{query.Filter.Name}%"));
            }

            if (query.Filter.AntibioticGroupId is not null)
            {
                queryable = queryable.Where(x => x.AntibioticGroupId == query.Filter.AntibioticGroupId);
            }

            if (query.Filter.Classification is not null)
            {
                queryable = queryable.Where(x => x.Classification == query.Filter.Classification);
            }
        }

        // Get paged antibiotics
        var antibiotics = await queryable
            .AsNoTracking()
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new PagedAntibioticItem()
            {
                Id = x.Id,
                Name = x.Name,
                AntibioticGroup = new AntibioticGroupResult()
                {
                    Id = x.AntibioticGroupId,
                    Name = x.AntibioticGroup.Name,
                },
                Classification = x.Classification
            })
            .ToPagedListAsync(query.Param.Page, query.Param.Size);
        return factory.Create(antibiotics);
    }
}
