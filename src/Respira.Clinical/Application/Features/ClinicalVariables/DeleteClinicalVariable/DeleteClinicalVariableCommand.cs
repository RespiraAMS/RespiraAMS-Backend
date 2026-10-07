using Respira.ServiceDefaults.Contracts.CQRS;

namespace Respira.Clinical.Application.Features.ClinicalVariables.DeleteClinicalVariable
{
    public record DeleteClinicalVariableCommand : ICommand
    {
        /// <summary>
        /// Clinical variable ID
        /// </summary>
        public required Guid Id { get; set; }
    }
}
