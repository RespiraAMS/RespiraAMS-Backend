namespace Application.Contracts.Messages;

/// <summary>
/// Reply payload from the Media service for a <see cref="GetMediaQuery"/>.
/// </summary>
public record GetMediaResult
{
    public required string Url { get; set; }
}
