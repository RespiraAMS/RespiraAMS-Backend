using Respira.ServiceDefaults.Contracts.CQRS;
using Respira.ServiceDefaults.Contracts.Pagination;

namespace Respira.Clinical.Application.Features.RiskFactors.GetPagedRiskFactor
{
    public record RiskFactorFilter
    {
        /// <summary>
        /// Pathogen ID
        /// </summary>
        public Guid? PathogenId { get; set; }
    }

    public record GetPagedRiskFactorQuery : IQuery
    {
        /// <summary>
        /// Pagination parameter
        /// </summary>
        public required PaginationParam Param { get; set; }

        /// <summary>
        /// Risk factor filter
        /// </summary>
        public RiskFactorFilter? Filter { get; set; }
    }

    /// <summary>
    /// Pathogen result
    /// </summary>
    /// <param name="Id">Pathogen ID</param>
    /// <param name="Name">Pathogen name</param>
    public record PathogenResult(Guid Id, string Name);
    /// <summary>
    /// Criterion result
    /// </summary>
    /// <param name="Id">Criterion ID</param>
    /// <param name="Name">Criterion name</param>
    /// <param name="Formula">Criterion formula</param>
    public record CriterionResult(Guid Id, string Name, string Formula);

    public record PagedRiskFactorItem
    {
        /// <summary>
        /// Risk factor ID
        /// </summary>
        public required Guid Id { get; set; }

        /// <summary>
        /// Pathogen that associated with this risk factor
        /// </summary>
        public required PathogenResult Pathogen { get; set; }

        /// <summary>
        /// Risk factor criterion
        /// </summary>
        public required CriterionResult Criterion { get; set; }
    }
}
