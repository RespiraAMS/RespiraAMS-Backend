using Respira.ServiceDefaults.Contracts.CQRS;

namespace Respira.Clinical.Application.Features.RiskFactors.GetRiskFactorById
{
    public record GetRiskFactorByIdQuery : IQuery
    {
        public required Guid Id { get; set; }
    }

    public record PathogenResult(Guid Id, string Name, bool IsAtypical);
    public record CriterionResult(Guid Id, string Name, string Formula);
    public record RiskFactorResult
    {
        public required Guid Id { get; set; }
        public required PathogenResult Pathogen { get; set; }
        public required CriterionResult Criterion { get; set; }
    }
}
