using Respira.ServiceDefaults.Constracts.CQRS;

namespace Application.Features.Media.Create.Commands;

/// <summary>
/// Creates a media asset. The file is validated (must be an image) and, when valid,
/// uploaded to Cloudflare R2. The resulting asset stores the public URL.
/// </summary>
public record CreateMediaCommand : ICommand
{
    public required Guid SagaId { get; init; }
    public required Guid MediaId { get; init; }
    public required string FileName { get; init; }
    public required string ContentType { get; init; }
    public long Size { get; init; }
    public required byte[] Data { get; init; }
}
