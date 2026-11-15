namespace Respira.Clinical.API.Dtos;

public sealed record RollbackRangeRequestDto
{
    public DateTimeOffset? From { get; init; }
    public DateTimeOffset? To { get; init; }
    public string? EntityType { get; init; }
}
