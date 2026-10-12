using Respira.ServiceDefaults.Contracts.CQRS;

namespace Respira.Clinical.Application.Features.ClinicalMetrics.DeleteClinicalMetric
{
    public record DeleteClinicalMetricCommand : ICommand
    {
        /// <summary>
        /// Clinical metric ID
        /// </summary>
        public required Guid Id { get; set; }
    }
}
