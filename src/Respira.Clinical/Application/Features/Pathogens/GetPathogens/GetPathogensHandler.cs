using Application.Contracts.Data;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Pathogens.GetPathogens;

public class GetPathogensHandler(IDbContext context) : IQueryHandler<GetPathogensQuery, ICollection<GetPathogensResult>>
{
    public async Task<ICollection<GetPathogensResult>> HandleAsync(GetPathogensQuery query,
        CancellationToken cancellationToken = default)
    {
        var pathogens = await context.Pathogens
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new GetPathogensResult()
            {
                Id = x.Id,
                Name = x.Name,
            })
            .ToListAsync(cancellationToken);
        return pathogens;
    }
}