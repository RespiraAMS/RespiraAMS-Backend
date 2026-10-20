using Respira.ServiceDefaults.Contracts.CQRS;
using Respira.ServiceDefaults.Contracts.Pagination;

namespace Authentication.Application.Features.Authentication.Queries.GetList
{
    public record GetListAccountQuery : IQuery
    {
        public PaginationParam PaginationParam { get; init; } = new();
    }
}
