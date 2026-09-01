using Domain.Entities;
using Respira.ServiceDefaults.Contracts.CQRS;

namespace Respira.SagaAudit.Application.Features.ListSagas.Queries;

/// <summary>
/// Query for recent sagas with an optional status filter.
/// </summary>
public record ListSagasQuery : IQuery
{
    /// <summary>Optional status filter.</summary>
    public SagaStatus? Status { get; set; }

    /// <summary>Maximum number of sagas to return.</summary>
    public int Limit { get; set; } = 20;
}
