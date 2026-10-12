using Respira.Clinical.Application.Features.Shared.ManageFormula;
using Respira.ServiceDefaults.Contracts.CQRS;

namespace Respira.Clinical.Application.Features.ClinicalMetrics.UpdateRule
{
    public record UpdateRuleCommand : ICommand
    {
        public required Guid Id { get; set; }
        public required Guid ClinicalMetricId { get; set; }
        public required Guid CriterionId { get; set; }
        public FormulaDto? ScoreFunction { get; set; }
        public bool? IsMajor { get; set; }

    }
}
