using Respira.ServiceDefaults.Contracts.CQRS;

namespace Respira.Clinical.Application.Features.ClinicalMetrics.DeleteRule
{
    public record DeleteRuleCommand : ICommand
    {
        public required Guid Id { get; set; }
    }
}
