using Respira.ServiceDefaults.Contracts.CQRS;
using Respira.ServiceDefaults.Contracts.Pagination;

namespace Respira.Clinical.Application.Features.Criteria.GetPagedCriterion
{
    public record GetPagedCriterionQuery : IQuery
    {
        public required PaginationParam Param { get; set; }
    }

    public record PagedCriterionItem(Guid Id, string Name, string Formula);
}
