using Respira.ServiceDefaults.Constracts.CQRS;

namespace Application.Features.Media.Remove.Commands;

/// <summary>
/// Soft-deletes a media asset and removes its object from Cloudflare R2.
/// </summary>
public record RemoveMediaCommand : ICommand
{
    public required Guid SagaId { get; init; }
    public required Guid MediaId { get; init; }
}
