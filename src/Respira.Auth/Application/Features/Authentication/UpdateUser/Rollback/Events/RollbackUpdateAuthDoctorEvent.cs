namespace Application.Features.Authentication.UpdateUser.Rollback.Events;

/// <summary>
/// Emitted when the rollback of an updated auth doctor account completed successfully.
/// </summary>
public record RollbackUpdateAuthDoctorSuccess
{
    /// <summary>Correlation identifier of the saga that requested the rollback.</summary>
    public required Guid SagaId { get; init; }

    /// <summary>Identifier of the auth doctor account that was rolled back.</summary>
    public required Guid AuthUserId { get; init; }
}

/// <summary>
/// Emitted when rolling back an updated auth doctor account failed (exception during revert).
/// </summary>
public record RollbackUpdateAuthDoctorFailure
{
    /// <summary>Correlation identifier of the saga that requested the rollback.</summary>
    public required Guid SagaId { get; init; }

    /// <summary>Identifier of the auth doctor account targeted by the rollback.</summary>
    public required Guid AuthUserId { get; init; }
}
