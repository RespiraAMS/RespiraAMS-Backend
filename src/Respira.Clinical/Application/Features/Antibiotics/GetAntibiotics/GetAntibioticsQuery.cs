using Respira.ServiceDefaults.Contracts.CQRS;

namespace Respira.Clinical.Application.Features.Antibiotics.GetAntibiotics
{
    public record GetAntibioticsQuery : IQuery;
    public record AntibioticItem(Guid Id, string Name);
    public record GetAntibioticsResult(IEnumerable<AntibioticItem> Antibiotics);
}
