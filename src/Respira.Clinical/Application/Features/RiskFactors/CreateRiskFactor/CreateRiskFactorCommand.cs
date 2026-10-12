using Respira.ServiceDefaults.Contracts.CQRS;

namespace Respira.Clinical.Application.Features.RiskFactors.CreateRiskFactor
{
    public record CreateRiskFactorCommand : ICommand
    {
        /// <summary>
        /// Pathogen ID
        /// </summary>
        public required Guid PathogenId { get; set; }

        /// <summary>
        /// Criterion ID
        /// </summary>
        public required Guid CriterionId { get; set; }
    }

    /// <summary>
    /// Create risk factor result
    /// </summary>
    /// <param name="Id">Created risk factor ID</param>
    public record CreateRiskFactorResult(Guid Id);
}
