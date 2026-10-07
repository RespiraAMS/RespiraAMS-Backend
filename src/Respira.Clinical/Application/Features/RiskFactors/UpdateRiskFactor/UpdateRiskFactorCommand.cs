using Respira.ServiceDefaults.Contracts.CQRS;

namespace Respira.Clinical.Application.Features.RiskFactors.UpdateRiskFactor
{
    public record UpdateRiskFactorCommand : ICommand
    {
        public required Guid Id { get; set; }
        public required Guid PathogenId { get; set; }
        public required Guid CriterionId { get; set; }
    }
}
