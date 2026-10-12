using Respira.ServiceDefaults.Contracts.CQRS;

namespace Respira.Clinical.Application.Features.ClinicalMetrics.DeleteMetricRule
{
    public record DeleteMetricRuleCommand : ICommand
    {
        public required Guid Id { get; set; }
    }
}
