using Application.Contracts.Data;
using Microsoft.EntityFrameworkCore;
using X.PagedList.EF;

namespace Application.Features.Antibiograms.GetPagedAntibiogram;

public class GetPagedAntibiogramHandler(IDbContext context, IPaginationFactory factory)
    : IQueryHandler<GetPagedAntibiogramQuery, Respira.ServiceDefaults.Contracts.Results.Result<Pagination<PagedAntibiogramItem>>>
{
    public async Task<Respira.ServiceDefaults.Contracts.Results.Result<Pagination<PagedAntibiogramItem>>> HandleAsync(GetPagedAntibiogramQuery query, CancellationToken cancellationToken = default)
    {
        // Apply filter
        var queryable = context.Antibiograms.AsQueryable();
        if (query.Filter?.PathogenId is not null)
        {
            queryable = queryable.Where(x => x.PathogenId == query.Filter.PathogenId);
        }

        // Get paged antibiogram
#pragma warning disable RCS1077 // Optimize LINQ method call: ConvertAll won't work with EF Core SQL translation
        var antibiograms = await queryable
            .AsNoTracking()
            .AsSplitQuery()
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
                Mics = x.Mics.Select(m => new AntibioticResult
                {
                    Id = m.Id,
                    Name = m.Name
                }).ToList(),
                FirstPriorityMedicines = x.FirstPriorityMedicines.Select(m => new AntibioticResult
                {
                    Id = m.Id,
                    Name = m.Name
                }).ToList(),
                SecondPriorityMedicines = x.SecondPriorityMedicines.Select(m => new AntibioticResult
                {
                    Id = m.Id,
                    Name = m.Name
                }).ToList(),
            })
            .ToPagedListAsync(query.Param.Page, query.Param.Size);
#pragma warning restore RCS1077 // Optimize LINQ method call
        return Respira.ServiceDefaults.Contracts.Results.Result<Pagination<PagedAntibiogramItem>>.Success(Status.Success, factory.Create(antibiograms));
    }
}
