using Respira.Clinical.Application.Features.ClinicalMetrics.AddMetricRule;
using Respira.Clinical.Application.Features.ClinicalMetrics.UpdateMetricRule;
using Respira.Clinical.Application.Features.Shared.ManageFormula;
using Respira.Clinical.Domain.Entities;

namespace Respira.Clinical.API.Dtos
{
    public record AddMetricRuleRequestDto
    {
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
        /// <summary>
        /// Metric rule ID
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
