using Respira.ServiceDefaults.Contracts.CQRS;

namespace Respira.Clinical.Application.Features.ClinicalMetrics.UpdateClinicalMetric
{
    public record UpdateClinicalMetricCommand : ICommand
    {
        public required Guid Id { get; set; }
        public required string Name { get; set; }
        public required string Code { get; set; }
        public required string Description { get; set; }
        public required string Version { get; set; }
        public required string Publisher { get; set; }
        public required bool IsApplied { get; set; }
    }
}
