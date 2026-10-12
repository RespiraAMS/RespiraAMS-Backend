using Respira.Clinical.Application.Features.Shared.ManageFormula;
using Respira.Clinical.Domain.Entities;
using Respira.ServiceDefaults.Contracts.CQRS;

namespace Respira.Clinical.Application.Features.ClinicalMetrics.AddMetricRule
{
    public record AddMetricRuleCommand : ICommand
    {
        /// <summary>
        /// Clinical metric ID
        /// </summary>
        public required Guid ClinicalMetricId { get; set; }

        /// <summary>
        /// Criterion ID
        /// </summary>
        public required Guid CriterionId { get; set; }

        /// <summary>
        /// Score function of this rule. Only provided this value if this is a <see cref="ScoringRule"/>
        /// </summary>
        public FormulaDto? ScoreFunction { get; set; }

        /// <summary>
        /// Is this a major rule or a minor rule. Only provided this value if this is a <see cref="MajorMinorRule"/>
        /// </summary>
        public bool? IsMajor { get; set; }
    }

    /// <summary>
    /// Add metric rule result
    /// </summary>
    /// <param name="Id">Created metric rule ID</param>
    public record AddMetricRuleResult(Guid Id);
}
