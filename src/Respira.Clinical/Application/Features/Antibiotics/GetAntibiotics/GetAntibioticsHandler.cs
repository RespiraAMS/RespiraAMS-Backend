using Application.Contracts.Data;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Antibiotics.GetAntibiotics;

public class GetAntibioticsHandler(IDbContext context)
    : IQueryHandler<GetAntibioticsQuery, Respira.ServiceDefaults.Contracts.Results.Result<GetAntibioticsResult>>
{
    public async Task<Respira.ServiceDefaults.Contracts.Results.Result<GetAntibioticsResult>> HandleAsync(GetAntibioticsQuery query, CancellationToken cancellationToken = default)
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
        return Respira.ServiceDefaults.Contracts.Results.Result<GetAntibioticsResult>.Success(Status.Success, new GetAntibioticsResult(antibiotics));
    }
}
