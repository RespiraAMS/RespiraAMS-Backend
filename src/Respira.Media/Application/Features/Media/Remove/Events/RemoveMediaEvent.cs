namespace Application.Features.Media.Remove.Events;

public record RemoveMediaSuccess
{
    public required Guid SagaId { get; init; }
    public required Guid MediaId { get; init; }
}

public record RemoveMediaFailure
{
    public required Guid SagaId { get; init; }
    public required Guid MediaId { get; init; }
    public required string Message { get; init; }
}
