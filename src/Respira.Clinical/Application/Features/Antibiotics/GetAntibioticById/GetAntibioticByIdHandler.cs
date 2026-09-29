using Microsoft.EntityFrameworkCore;
using Respira.Clinical.Application.Contracts.Data;
using Respira.ServiceDefaults.Contracts.CQRS;
using Respira.ServiceDefaults.Contracts.Results;

namespace Respira.Clinical.Application.Features.Antibiotics.GetAntibioticById
{
    public class GetAntibioticByIdHandler(IDbContext context)
    : IQueryHandler<GetAntibioticByIdQuery, Result<AntibioticResult>>
    {
        public async Task<Result<AntibioticResult>> HandleAsync(GetAntibioticByIdQuery query, CancellationToken cancellationToken = default)
        {
            var antibiotic = await context.Antibiotics
                .AsNoTracking()
                .Select(x => new AntibioticResult
                {
                    Id = x.Id,
                    Name = x.Name,
                    AntibioticGroup = new AntibioticGroupResult
                    {
                        Id = x.AntibioticGroup.Id,
                        Name = x.AntibioticGroup.Name,
                        Description = x.AntibioticGroup.Description,
                        ParentId = x.AntibioticGroup.ParentId,
                        ParentName = x.AntibioticGroup.Parent == null ? null : x.AntibioticGroup.Parent.Name
                    },
                    Classification = x.Classification,
                    Dosages = x.Dosages.Select(d => new DosageResult
                    {
                        Id = d.Id,
                        RouteOfAdministration = d.RouteOfAdministration,
                        Dose = d.Dose,
                        Crcl = d.Crcl
                    }).ToList(),
                })
                .FirstOrDefaultAsync(x => x.Id == query.Id, cancellationToken);

            return antibiotic is null
                ? Result<AntibioticResult>.Failure(new Error(ApplicationStatus.ResourceNotFound, "Antibiotic not found"))
                : Result<AntibioticResult>.Success(ApplicationStatus.Success, antibiotic);
        }
    }
}
