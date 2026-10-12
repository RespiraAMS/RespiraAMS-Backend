using Respira.ServiceDefaults.Contracts.CQRS;

namespace Respira.Clinical.Application.Features.RiskFactors.UpdateRiskFactor
{
    public record UpdateRiskFactorCommand : ICommand
    {
        /// <summary>
        /// Risk factor ID
        /// </summary>
        public required Guid Id { get; set; }

        /// <summary>
        /// Pathogen ID
        /// </summary>
        public required Guid PathogenId { get; set; }

        /// <summary>
        /// Risk factor criterion ID
        /// </summary>
        public required Guid CriterionId { get; set; }
    }
}
