using Microsoft.EntityFrameworkCore;
using Respira.Clinical.Application.Contracts.Data;
using Respira.ServiceDefaults.Contracts.CQRS;
using Respira.ServiceDefaults.Contracts.Results;

namespace Respira.Clinical.Application.Features.Antibiotics.GetAntibiotics
{
    public class GetAntibioticsHandler(IDbContext context) : IQueryHandler<GetAntibioticQuery, Result<GetAntibioticsResult>>
    {
        public async Task<Result<GetAntibioticsResult>> HandleAsync(GetAntibioticQuery query, CancellationToken cancellationToken = default)
        {
            var antibiotics = await context.Antibiotics
                .AsNoTracking()
                .OrderBy(x => x.Name)
                .Select(x => new AntibioticItem(x.Id, x.Name))
                .ToListAsync(cancellationToken);
            return Result<GetAntibioticsResult>.Success(ApplicationStatus.Success, new GetAntibioticsResult(antibiotics));
        }
    }
}
