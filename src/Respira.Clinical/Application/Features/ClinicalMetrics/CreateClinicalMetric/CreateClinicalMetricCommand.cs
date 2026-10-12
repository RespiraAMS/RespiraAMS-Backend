using Respira.ServiceDefaults.Contracts.CQRS;

namespace Respira.Clinical.Application.Features.ClinicalMetrics.CreateClinicalMetric
{
    public record CreateClinicalMetricCommand : ICommand
    {
        public required string Name { get; set; }
        public required string Code { get; set; }
        public required string Description { get; set; }
        public required string Version { get; set; }
        public required string Publisher { get; set; }
    }

    public record CreateClinicalMetricResult(Guid Id);
}
