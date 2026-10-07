using Respira.ServiceDefaults.Contracts.CQRS;

namespace Respira.Clinical.Application.Features.ClinicalVariables.GetClinicalVariables
{
    public record GetClinicalVariablesQuery : IQuery;

    public record ClinicalVariableItem(Guid Id, string Name, string Code);
    public record GetClinicalVariablesResult(IEnumerable<ClinicalVariableItem> ClinicalVariables);
}
