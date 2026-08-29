namespace Application.Features.Authentication.DeleteUser.Events;

/// <summary>
/// Emitted when an auth doctor account was successfully deleted as part of the DeleteUser saga.
/// </summary>
public record DeleteAuthDoctorSuccess
{
    /// <summary>Correlation identifier of the saga that issued the delete.</summary>
    public required Guid SagaId { get; init; }

    /// <summary>Identifier of the auth doctor account that was deleted.</summary>
    public required Guid AuthUserId { get; init; }
}

/// <summary>
/// Emitted when deleting an auth doctor account failed (exception during removal).
/// </summary>
public record DeleteAuthDoctorFailure
{
    /// <summary>Correlation identifier of the saga that issued the delete.</summary>
    public required Guid SagaId { get; init; }

    /// <summary>Identifier of the auth doctor account targeted by the delete.</summary>
    public required Guid AuthUserId { get; init; }

    /// <summary>Human-readable failure reason.</summary>
    public required string Message { get; init; }
}
