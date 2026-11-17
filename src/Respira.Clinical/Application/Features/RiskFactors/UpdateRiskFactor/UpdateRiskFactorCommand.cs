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

        /// <summary>
        /// Is this risk factor critical? A critical risk factor is more credible
        /// than a non-critical risk factor.when assessing for risk infection
        /// </summary>
        public required bool IsCriticalFactor { get; set; }
    }
}
