using Respira.ServiceDefaults.Contracts.CQRS;

namespace Respira.Clinical.Application.Features.ClinicalMetrics.CreateClinicalMetric
{
    public record CreateClinicalMetricCommand : ICommand
    {
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
    }

    /// <summary>
    /// Create clinical metric result
    /// </summary>
    /// <param name="Id">Created clinical metric ID</param>
    public record CreateClinicalMetricResult(Guid Id);
}
