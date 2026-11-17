using Respira.ServiceDefaults.Contracts.CQRS;

namespace Respira.Clinical.Application.Features.ClinicalMetrics.UpdateClinicalMetric
{
    public record UpdateClinicalMetricCommand : ICommand
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
        public required bool IsApplied { get; set; }
    }
}
