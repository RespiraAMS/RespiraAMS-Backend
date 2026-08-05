using Application.Contracts.Data;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Antibiotics.GetAntibioticById;

public class GetAntibioticByIdHandler(IDbContext context) : IQueryHandler<GetAntibioticByIdQuery, AntibioticResult>
{
    public async Task<AntibioticResult> HandleAsync(GetAntibioticByIdQuery query,
        CancellationToken cancellationToken = default)
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
                    ParentId = x.AntibioticGroupId,
                    ParentName = x.AntibioticGroup.Parent == null ? null : x.AntibioticGroup.Parent.Name
                },
                Category = x.Category,
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
                    GlomerularFiltrationRate = d.GlomerularFiltrationRate
                }).ToList(),
            })
            .FirstOrDefaultAsync(x => x.Id == query.Id, cancellationToken);
        return antibiotic ?? throw new NotFoundException(nameof(Antibiotic), query.Id);
    }
}