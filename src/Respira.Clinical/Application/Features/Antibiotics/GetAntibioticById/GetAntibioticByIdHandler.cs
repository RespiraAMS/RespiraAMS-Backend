using Application.Contracts.Data;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Antibiotics.GetAntibioticById;

public class GetAntibioticByIdHandler(IDbContext context)
    : IQueryHandler<GetAntibioticByIdQuery, Result<AntibioticResult>>
{
    public async Task<Result<AntibioticResult>> HandleAsync(GetAntibioticByIdQuery query, CancellationToken cancellationToken = default)
    {
#pragma warning disable RCS1077 // Optimize LINQ method call: ConvertAll won't work with EF Core SQL translation
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
                AntibioticSpectrum = x.AntibioticSpectra.Select(a => new PathogenResult
                {
                    Id = a.Id,
                    Name = a.Name,
                }).ToList(),
                Dosages = x.Dosages.Select(d => new DosageResult
                {
                    Id = d.Id,
                    RouteOfAdministration = d.RouteOfAdministration,
                    Dose = d.Dose,
                    Crcl = d.Crcl
                }).ToList(),
            })
            .FirstOrDefaultAsync(x => x.Id == query.Id, cancellationToken);
#pragma warning restore RCS1077 // Optimize LINQ method call

        return antibiotic is null
            ? Result<AntibioticResult>.Failure(new Error(Status.ResourceNotFound, "Antibiotic not found"))
            : Result<AntibioticResult>.Success(Status.Success, antibiotic);
    }
}
