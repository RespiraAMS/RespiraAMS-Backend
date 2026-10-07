using Respira.ServiceDefaults.Contracts.CQRS;

namespace Respira.Clinical.Application.Features.RiskFactors.CreateRiskFactor
{
    public record CreateRiskFactorCommand : ICommand
    {
        public required Guid PathogenId { get; set; }
        public required Guid CriterionId { get; set; }
    }

    public record CreateRiskFactorResult(Guid Id);
}
