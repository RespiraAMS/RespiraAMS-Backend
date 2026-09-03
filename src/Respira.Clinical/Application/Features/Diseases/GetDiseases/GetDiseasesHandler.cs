using Application.Contracts.Data;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Diseases.GetDiseases;

public class GetDiseasesHandler(IDbContext context) : IQueryHandler<GetDiseasesQuery, Respira.ServiceDefaults.Contracts.Results.Result<GetDiseasesResult>>
{
    public async Task<Respira.ServiceDefaults.Contracts.Results.Result<GetDiseasesResult>> HandleAsync(GetDiseasesQuery query,
        CancellationToken cancellationToken = default)
    {
        var diseases = await context.Diseases
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .Select(x => new DiseaseItem
            {
                Id = x.Id,
                Name = x.Name,
            })
            .ToListAsync(cancellationToken);
        return Respira.ServiceDefaults.Contracts.Results.Result<GetDiseasesResult>.Success(Status.Success, new GetDiseasesResult(diseases));
    }
}
