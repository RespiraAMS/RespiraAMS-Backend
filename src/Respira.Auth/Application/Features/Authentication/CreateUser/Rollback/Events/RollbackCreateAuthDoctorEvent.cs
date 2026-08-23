namespace Application.Features.Authentication.CreateUser.Rollback.Events;

/// <summary>
/// Emitted when the rollback of a created auth doctor account completed successfully.
/// </summary>
public record RollbackCreateAuthDoctorSuccess
{
    /// <summary>Correlation identifier of the saga that requested the rollback.</summary>
    public required Guid SagaId { get; init; }

    /// <summary>Identifier of the auth doctor account that was rolled back.</summary>
    public required Guid AuthUserId { get; init; }
}

/// <summary>
/// Emitted when rolling back a created auth doctor account failed (exception during removal).
/// </summary>
public record RollbackCreateAuthDoctorFailure
{
    /// <summary>Correlation identifier of the saga that requested the rollback.</summary>
    public required Guid SagaId { get; init; }

    /// <summary>Identifier of the auth doctor account targeted by the rollback.</summary>
    public required Guid AuthUserId { get; init; }
}
