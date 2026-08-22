namespace Application.Features.Media.Create.Events;

public record CreateMediaSuccess
{
    public required Guid SagaId { get; init; }
    public required Guid MediaId { get; init; }
    public string? Url { get; init; }
}

public record CreateMediaFailure
{
    public required Guid SagaId { get; init; }
    public required string Message { get; init; }
}
