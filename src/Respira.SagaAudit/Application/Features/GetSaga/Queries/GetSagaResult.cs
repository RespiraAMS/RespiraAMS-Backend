namespace Respira.SagaAudit.Application.Features.GetSaga.Queries;

/// <summary>
/// ApiResponse of a <see cref="GetSagaQuery"/>: a full saga execution snapshot.
/// </summary>
public record GetSagaResult
{
    /// <summary>Saga instance ID.</summary>
    public required Guid SagaId { get; init; }

    /// <summary>Type of saga (CreateDoctor, UpdateDoctor, DeleteDoctor).</summary>
    public required string SagaType { get; init; }

    /// <summary>Current status: Running, Completed, Failed, Compensating.</summary>
    public required string Status { get; init; }

    /// <summary>Current step being executed.</summary>
    public string? CurrentStep { get; init; }

    /// <summary>Completed steps with timestamps.</summary>
    public object[]? Steps { get; init; }

    /// <summary>Failure reason if the saga failed.</summary>
    public string? FailureReason { get; init; }

    /// <summary>ID of the manager who initiated the saga.</summary>
    public Guid? ManagerId { get; init; }

    /// <summary>ID of the doctor being created/updated/deleted.</summary>
    public Guid? TargetDoctorId { get; init; }

    /// <summary>When the saga tracking record was created.</summary>
    public required DateTimeOffset CreatedAt { get; init; }

    /// <summary>When the saga tracking record was last updated.</summary>
    public required DateTimeOffset UpdatedAt { get; init; }
}
