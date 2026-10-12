using Respira.ServiceDefaults.Contracts.CQRS;

namespace Respira.Clinical.Application.Features.ClinicalMetrics.DeleteClinicalMetric
{
    public record DeleteClinicalMetricCommand : ICommand
    {
        public required Guid Id { get; set; }
    }
}
