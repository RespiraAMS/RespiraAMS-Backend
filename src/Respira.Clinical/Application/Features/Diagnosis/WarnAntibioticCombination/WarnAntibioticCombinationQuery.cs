using Respira.ServiceDefaults.Contracts.CQRS;

namespace Respira.Clinical.Application.Features.Diagnosis.WarnAntibioticCombination
{
    public record WarnAntibioticCombinationQuery : IQuery
    {
        public required List<Guid> AntibioticIds { get; set; }
    }

    public record WarnAntibioticCombinationResult(bool NeedWarning);
}
