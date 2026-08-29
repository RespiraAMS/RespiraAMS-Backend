namespace Application.Features.Media.Remove.Events;

/// <summary>
/// Published when a media asset has been successfully removed (or was already absent),
/// allowing the orchestrating saga to proceed.
/// </summary>
public record RemoveMediaSuccess
{
    /// <summary>Identifier of the saga that requested the removal.</summary>
    public required Guid SagaId { get; init; }

    /// <summary>Identifier of the media asset that was removed.</summary>
    public required Guid MediaId { get; init; }
}

/// <summary>
/// Published when removal of a media asset fails, allowing the orchestrating saga to compensate.
/// </summary>
public record RemoveMediaFailure
{
    /// <summary>Identifier of the saga that requested the removal.</summary>
    public required Guid SagaId { get; init; }

    /// <summary>Identifier of the media asset that could not be removed.</summary>
    public required Guid MediaId { get; init; }

    /// <summary>Human-readable description of the failure.</summary>
    public required string Message { get; init; }
}
