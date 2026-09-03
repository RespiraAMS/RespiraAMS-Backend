using Application.Contracts.Data;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Pathogens.GetPathogens;

public class GetPathogensHandler(IDbContext context) : IQueryHandler<GetPathogensQuery, Respira.ServiceDefaults.Contracts.Results.Result<GetPathogensResult>>
{
    public async Task<Respira.ServiceDefaults.Contracts.Results.Result<GetPathogensResult>> HandleAsync(GetPathogensQuery query,
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
        return Respira.ServiceDefaults.Contracts.Results.Result<GetPathogensResult>.Success(Status.Success, new GetPathogensResult(pathogens));
    }
}
