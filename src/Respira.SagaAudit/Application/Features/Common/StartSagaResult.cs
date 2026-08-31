namespace Respira.SagaAudit.Application.Features.Common;

/// <summary>Result of starting a saga: the id used to track its execution.</summary>
/// <param name="SagaId">Id of the started saga.</param>
public record StartSagaResult(Guid SagaId);
