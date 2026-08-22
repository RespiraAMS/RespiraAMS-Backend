using Respira.ServiceDefaults.Constracts.CQRS;

namespace Application.Features.Media.Update.Commands;

/// <summary>
/// Replaces a media asset's file. The new file is validated (must be an image) and,
/// when valid, uploaded to Cloudflare R2; the previous object is removed.
/// </summary>
public record UpdateMediaCommand : ICommand
{
    public required Guid SagaId { get; init; }
    public required Guid MediaId { get; init; }
    public required string FileName { get; init; }
    public required string ContentType { get; init; }
    public long Size { get; init; }
    public required byte[] Data { get; init; }
}
