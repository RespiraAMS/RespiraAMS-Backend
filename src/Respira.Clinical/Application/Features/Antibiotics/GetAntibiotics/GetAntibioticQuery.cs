using Respira.ServiceDefaults.Contracts.CQRS;

namespace Respira.Clinical.Application.Features.Antibiotics.GetAntibiotics
{
    public record GetAntibioticQuery : IQuery;
    public record AntibioticItem(Guid Id, string Name);
    public record GetAntibioticsResult(IEnumerable<AntibioticItem> Antibiotics);
}
