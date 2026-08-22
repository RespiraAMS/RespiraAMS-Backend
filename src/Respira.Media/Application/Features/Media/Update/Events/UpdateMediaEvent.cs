namespace Application.Features.Media.Update.Events;

public record UpdateMediaSuccess
{
    public required Guid SagaId { get; init; }
    public required Guid MediaId { get; init; }
    public string? Url { get; init; }
}

public record UpdateMediaFailure
{
    public required Guid SagaId { get; init; }
    public required Guid MediaId { get; init; }
    public required string Message { get; init; }
}
