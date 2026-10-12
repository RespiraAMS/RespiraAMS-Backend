using Respira.Clinical.Domain.Entities;
using Respira.ServiceDefaults.Contracts.CQRS;

namespace Respira.Clinical.Application.Features.ClinicalMetrics.GetMetricRuleById
{
    public record GetMetricRuleByIdQuery : IQuery
    {
        /// <summary>
        /// Metric rule ID
        /// </summary>
        public required Guid Id { get; set; }
    }

    /// <summary>
    /// Clinical metric's criterion result
    /// </summary>
    /// <param name="Id">Criterion ID</param>
    /// <param name="Name">Criterion name</param>
    /// <param name="Formula">Criterion formula</param>
    public record CriterionResult(Guid Id, string Name, string Formula);

    public record MetricRuleResult
    {
        /// <summary>
        /// Metric rule ID
        /// </summary>
        public required Guid Id { get; set; }

        /// <summary>
        /// Clinical metric ID
        /// </summary>
        public required Guid ClinicalMetricId { get; set; }

        /// <summary>
        /// Clinical metric name
        /// </summary>
        public required string ClinicalMetricName { get; set; }

        /// <summary>
        /// Metric rule's criterion
        /// </summary>
        public required CriterionResult Criterion { get; set; }

        /// <summary>
        /// Metric rule's score function (if <see cref="ScoringRule"/>)
        /// </summary>
        public string? ScoreFunction { get; set; }

        /// <summary>
        /// Is this a major rule or a minor rule (if <see cref="MajorMinorRule"/>)
        /// </summary>
        public bool? IsMajor { get; set; }
    }
}
