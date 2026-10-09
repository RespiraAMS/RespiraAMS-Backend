using Respira.ServiceDefaults.Contracts.CQRS;
using Respira.ServiceDefaults.Contracts.Pagination;

namespace Respira.Clinical.Application.Features.RiskFactors.GetPagedRiskFactor
{
    public record RiskFactorFilter
    {
        public Guid? PathogenId { get; set; }
    }

    public record GetPagedRiskFactorQuery : IQuery
    {
        public required PaginationParam Param { get; set; }
        public RiskFactorFilter? Filter { get; set; }
    }

    public record PathogenResult(Guid Id, string Name);

    public record CriterionResult(Guid Id, string Name, string Formula);

    public record PagedRiskFactorItem
    {
        public required Guid Id { get; set; }
        public required PathogenResult Pathogen { get; set; }
        public required CriterionResult Criterion { get; set; }
    }
}
