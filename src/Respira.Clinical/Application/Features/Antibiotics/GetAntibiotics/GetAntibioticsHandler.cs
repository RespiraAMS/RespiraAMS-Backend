using Application.Contracts.Data;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Antibiotics.GetAntibiotics;

public class GetAntibioticsHandler(IDbContext context) : IQueryHandler<GetAntibioticsQuery, GetAntibioticsResult>
{
    public async Task<GetAntibioticsResult> HandleAsync(GetAntibioticsQuery query,
        CancellationToken cancellationToken = default)
    {
        var antibiotics = await context.Antibiotics
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .Select(x => new AntibioticItem
            {
                Id = x.Id,
                Name = x.Name
            })
            .ToListAsync(cancellationToken);
        return new GetAntibioticsResult(antibiotics);
    }
}