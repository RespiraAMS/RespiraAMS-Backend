namespace Application.Features.Authentication.CreateUser.Events;

/// <summary>
/// Emitted when an auth doctor account was successfully created as part of the CreateUser saga.
/// </summary>
public record CreateAuthDoctorSuccess
{
    /// <summary>Correlation identifier of the saga that issued the create.</summary>
    public required Guid SagaId { get; init; }

    /// <summary>Identifier of the created auth doctor account.</summary>
    public required Guid AuthUserId { get; init; }
}

/// <summary>
/// Emitted when creating an auth doctor account failed (e.g. duplicate email or exception).
/// </summary>
public record CreateAuthDoctorFailure
{
    /// <summary>Correlation identifier of the saga that issued the create.</summary>
    public required Guid SagaId { get; init; }

    /// <summary>Human-readable failure reason.</summary>
    public required string Message { get; init; }
}
