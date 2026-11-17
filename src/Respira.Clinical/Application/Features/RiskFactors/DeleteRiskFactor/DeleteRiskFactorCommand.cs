using Respira.ServiceDefaults.Contracts.CQRS;

namespace Respira.Clinical.Application.Features.RiskFactors.DeleteRiskFactor
{
    public record DeleteRiskFactorCommand : ICommand
    {
        /// <summary>
        /// Risk factor ID
        /// </summary>
        public required Guid Id { get; set; }
    }
}
