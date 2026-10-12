using Respira.Clinical.Domain.Entities;
using Respira.ServiceDefaults.Contracts.CQRS;

namespace Respira.Clinical.Application.Features.ClinicalMetrics.GetClinicalMetricById
{
    public record GetClinicalMetricByIdQuery : IQuery
    {
        /// <summary>
        /// Clinical metric ID
        /// </summary>
        public required Guid Id { get; set; }
    }

    /// <summary>
    /// Clinical metric's criterion result
    /// </summary>
    /// <param name="Id">Criterion ID</param>
    /// <param name="Name">Criterion name</param>
    /// <param name="Formula">Criterion formula</param>
    public record CriterionItem(Guid Id, string Name, string Formula);

    public record MetricRuleItem
    {
        /// <summary>
        /// Metric rule ID
        /// </summary>
        public required Guid Id { get; set; }

        /// <summary>
        /// Metric rule's criterion
        /// </summary>
        public required CriterionItem Criterion { get; set; }

        /// <summary>
        /// Metric rule's score function (if <see cref="ScoringRule"/>)
        /// </summary>
        public string? ScoreFunction { get; set; }

        /// <summary>
        /// Is this a major rule or a minor rule (if <see cref="MajorMinorRule"/>)
        /// </summary>
        public bool? IsMajorRule { get; set; }
    }

    public record ClinicalMetricResult
    {
        /// <summary>
        /// Clinical metric ID
        /// </summary>
        public required Guid Id { get; set; }

        /// <summary>
        /// Clinical metric name
        /// </summary>
        public required string Name { get; set; }

        /// <summary>
        /// Clinical metric code
        /// </summary>
        public required string Code { get; set; }

        /// <summary>
        /// Clinical metric description
        /// </summary>
        public required string Description { get; set; }

        /// <summary>
        /// Clinical metric version
        /// </summary>
        public required string Version { get; set; }

        /// <summary>
        /// Clinical metric publisher
        /// </summary>
        public required string Publisher { get; set; }

        /// <summary>
        /// Is clinical metric applied
        /// </summary>
        public bool IsApplied { get; set; }

        /// <summary>
        /// Clinical metric's rules
        /// </summary>
        public IEnumerable<MetricRuleItem> Rules { get; set; } = [];
    }
}
