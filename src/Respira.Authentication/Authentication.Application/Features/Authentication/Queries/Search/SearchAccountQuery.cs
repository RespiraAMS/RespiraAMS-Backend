using Respira.ServiceDefaults.Contracts.CQRS;

namespace Respira.Authentication.Application.Features.Authentication.Queries.Search
{
    public record SearchAccountQuery : IQuery
    {
        public required string Query { get; set; }
    }
}
