using Respira.Clinical.Application.Features.ClinicalMetrics.AddMetricRule;
using Respira.Clinical.Application.Features.ClinicalMetrics.UpdateMetricRule;
using Respira.Clinical.Application.Features.Shared.ManageFormula;

namespace Respira.Clinical.API.Dtos
{
    public record AddMetricRuleRequestDto
    {
        public required Guid CriterionId { get; set; }
        public FormulaDto? ScoreFunction { get; set; }
        public bool? IsMajor { get; set; }

        public AddMetricRuleCommand ToCommand(Guid clinicalMetricId)
        {
            return new AddMetricRuleCommand
            {
                ClinicalMetricId = clinicalMetricId,
                CriterionId = CriterionId,
                ScoreFunction = ScoreFunction,
                IsMajor = IsMajor,
            };
        }
    }

    public record UpdateMetricRuleRequestDto
    {
        public required Guid CriterionId { get; set; }
        public FormulaDto? ScoreFunction { get; set; }
        public bool? IsMajor { get; set; }

        public UpdateMetricRuleCommand ToCommand(Guid metricRuleId, Guid clinicalMetricId)
        {
            return new UpdateMetricRuleCommand
            {
                Id = metricRuleId,
                ClinicalMetricId = clinicalMetricId,
                CriterionId = CriterionId,
                ScoreFunction = ScoreFunction,
                IsMajor = IsMajor,
            };
        }
    }
}
