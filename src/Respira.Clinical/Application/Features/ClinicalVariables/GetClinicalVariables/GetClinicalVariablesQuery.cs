using Respira.ServiceDefaults.Contracts.CQRS;

namespace Respira.Clinical.Application.Features.ClinicalVariables.GetClinicalVariables
{
    public record GetClinicalVariablesQuery : IQuery;

    /// <summary>
    /// Clinical variable item
    /// </summary>
    /// <param name="Id">Clinical variable ID</param>
    /// <param name="Name">Clinical variable name</param>
    /// <param name="Code">Clinical variable code</param>
    public record ClinicalVariableItem(Guid Id, string Name, string Code);

    /// <summary>
    /// Clinical variable result
    /// </summary>
    /// <param name="ClinicalVariables">Clinical variables</param>
    public record GetClinicalVariablesResult(IEnumerable<ClinicalVariableItem> ClinicalVariables);
}
