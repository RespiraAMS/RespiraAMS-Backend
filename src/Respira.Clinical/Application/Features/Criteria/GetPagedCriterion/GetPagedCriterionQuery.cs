using Respira.ServiceDefaults.Contracts.CQRS;
using Respira.ServiceDefaults.Contracts.Pagination;

namespace Respira.Clinical.Application.Features.Criteria.GetPagedCriterion
{
    public record GetPagedCriterionQuery : IQuery
    {
        /// <summary>
        /// Pagination parameter
        /// </summary>
        public required PaginationParam Param { get; set; }
    }

    /// <summary>
    /// Criterion result
    /// </summary>
    /// <param name="Id">Criterion ID</param>
    /// <param name="Name">Criterion name</param>
    /// <param name="Formula">Criterion formula</param>
    public record PagedCriterionItem(Guid Id, string Name, string Formula);
}
