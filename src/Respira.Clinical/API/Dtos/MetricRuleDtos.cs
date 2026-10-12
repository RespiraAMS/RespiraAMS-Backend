using Respira.Clinical.Application.Features.ClinicalMetrics.AddRule;
using Respira.Clinical.Application.Features.ClinicalMetrics.UpdateRule;
using Respira.Clinical.Application.Features.Shared.ManageFormula;

namespace Respira.Clinical.API.Dtos
{
    public record AddRuleRequestDto
    {
        public required Guid CriterionId { get; set; }
        public FormulaDto? ScoreFunction { get; set; }
        public bool? IsMajor { get; set; }

        public AddRuleCommand ToCommand(Guid clinicalMetricId)
        {
            return new AddRuleCommand
            {
                ClinicalMetricId = clinicalMetricId,
                CriterionId = CriterionId,
                ScoreFunction = ScoreFunction,
                IsMajor = IsMajor,
            };
        }
    }

    public record UpdateRuleRequestDto
    {
        public required Guid CriterionId { get; set; }
        public FormulaDto? ScoreFunction { get; set; }
        public bool? IsMajor { get; set; }

        public UpdateRuleCommand ToCommand(Guid metricRuleId, Guid clinicalMetricId)
        {
            return new UpdateRuleCommand
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
