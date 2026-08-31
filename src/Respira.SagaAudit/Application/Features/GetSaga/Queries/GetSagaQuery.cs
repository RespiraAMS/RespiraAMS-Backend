using Respira.ServiceDefaults.Constracts.CQRS;

namespace Respira.SagaAudit.Application.Features.GetSaga.Queries;

/// <summary>
/// Query for the current execution state of a saga by its id.
/// </summary>
public record GetSagaQuery : IQuery
{
    /// <summary>Id of the saga to look up.</summary>
    public Guid SagaId { get; set; }
}
