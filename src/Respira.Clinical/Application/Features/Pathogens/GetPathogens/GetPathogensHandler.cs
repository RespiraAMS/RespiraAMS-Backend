using Application.Contracts.Data;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Pathogens.GetPathogens;

public class GetPathogensHandler(IDbContext context) : IQueryHandler<GetPathogensQuery, GetPathogensResult>
{
    public async Task<GetPathogensResult> HandleAsync(GetPathogensQuery query,
        CancellationToken cancellationToken = default)
    {
        var pathogens = await context.Pathogens
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .Select(x => new PathogenItem()
            {
                Id = x.Id,
                Name = x.Name,
            })
            .ToListAsync(cancellationToken);
        return new GetPathogensResult(pathogens);
    }
}