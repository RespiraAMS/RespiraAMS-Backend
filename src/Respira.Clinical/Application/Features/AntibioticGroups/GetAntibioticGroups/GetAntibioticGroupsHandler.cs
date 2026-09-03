using Application.Contracts.Data;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.AntibioticGroups.GetAntibioticGroups;

public class GetAntibioticGroupsHandler(IDbContext context)
    : IQueryHandler<GetAntibioticGroupsQuery, Respira.ServiceDefaults.Contracts.Results.Result<GetAntibioticGroupsResult>>
{
    public async Task<Respira.ServiceDefaults.Contracts.Results.Result<GetAntibioticGroupsResult>> HandleAsync(GetAntibioticGroupsQuery query, CancellationToken cancellationToken = default)
    {
        var group = await context.AntibioticGroups
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .Select(x => new AntibioticGroupItem()
            {
                Id = x.Id,
                Name = x.Name,
            })
            .ToListAsync(cancellationToken);
        return Respira.ServiceDefaults.Contracts.Results.Result<GetAntibioticGroupsResult>.Success(Status.Success, new GetAntibioticGroupsResult(group));
    }
}
