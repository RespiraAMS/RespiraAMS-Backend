using Application.Contracts.Data;
using Microsoft.EntityFrameworkCore;
using X.PagedList.EF;

namespace Application.Features.Antibiograms.GetPagedAntibiogram;

public class GetPagedAntibiogramHandler(IDbContext context, IPaginationFactory factory)
    : IQueryHandler<GetPagedAntibiogramQuery, Pagination<PagedAntibiogramItem>>
{
    public async Task<Pagination<PagedAntibiogramItem>> HandleAsync(GetPagedAntibiogramQuery query,
        CancellationToken cancellationToken = default)
    {
        // Apply filter
        var queryable = context.Antibiograms.AsQueryable();
        if (query.Filter?.PathogenId is not null)
        {
            queryable = queryable.Where(x => x.PathogenId == query.Filter.PathogenId);
        }

        // Get paged antibiogram
        var antibiograms = await queryable
            .AsNoTracking()
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new PagedAntibiogramItem
            {
                Id = x.Id,
                Pathogen = new PathogenResult
                {
                    Id = x.PathogenId,
                    Name = x.Pathogen.Name
                },
                MicLevel = x.MicLevel,
                Mics = x.Mics.ConvertAll(m => new AntibioticResult
                {
                    Id = m.Id,
                    Name = m.Name
                }),
                FirstPriorityMedicines = x.FirstPriorityMedicines.ConvertAll(m => new AntibioticResult
                {
                    Id = m.Id,
                    Name = m.Name
                }),
                SecondPriorityMedicines = x.SecondPriorityMedicines.ConvertAll(m => new AntibioticResult
                {
                    Id = m.Id,
                    Name = m.Name
                }),
            })
            .ToPagedListAsync(query.Param.Page, query.Param.Size);
        return factory.Create(antibiograms);
    }
}
