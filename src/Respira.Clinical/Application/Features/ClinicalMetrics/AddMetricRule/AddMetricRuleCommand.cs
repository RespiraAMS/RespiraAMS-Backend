using Respira.Clinical.Application.Features.Shared.ManageFormula;
using Respira.ServiceDefaults.Contracts.CQRS;

namespace Respira.Clinical.Application.Features.ClinicalMetrics.AddMetricRule
{
    public record AddMetricRuleCommand : ICommand
    {
        public required Guid ClinicalMetricId { get; set; }
        public required Guid CriterionId { get; set; }
        public FormulaDto? ScoreFunction { get; set; }
        public bool? IsMajor { get; set; }
    }

    public record AddRuleResult(Guid Id);
}
