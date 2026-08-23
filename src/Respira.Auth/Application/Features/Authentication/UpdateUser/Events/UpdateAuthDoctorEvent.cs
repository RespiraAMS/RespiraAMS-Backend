namespace Application.Features.Authentication.UpdateUser.Events;

/// <summary>
/// Emitted when an auth doctor account was successfully updated as part of the UpdateUser saga.
/// </summary>
public record UpdateAuthDoctorSuccess
{
    /// <summary>Correlation identifier of the saga that issued the update.</summary>
    public required Guid SagaId { get; init; }

    /// <summary>Identifier of the auth doctor account that was updated.</summary>
    public required Guid AuthUserId { get; init; }
}

/// <summary>
/// Emitted when updating an auth doctor account failed (account not found or exception).
/// </summary>
public record UpdateAuthDoctorFailure
{
    /// <summary>Correlation identifier of the saga that issued the update.</summary>
    public required Guid SagaId { get; init; }

    /// <summary>Human-readable failure reason.</summary>
    public required string Message { get; init; }
}
