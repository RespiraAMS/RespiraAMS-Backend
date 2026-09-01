using Respira.ServiceDefaults.Contracts.CQRS;

namespace Application.Features.Media.Remove.Commands;

/// <summary>
/// Soft-deletes a media asset and removes its object from Cloudflare R2.
/// </summary>
public record RemoveMediaCommand : ICommand
{
    /// <summary>Identifier of the saga orchestrating the media removal.</summary>
    public required Guid SagaId { get; init; }

    /// <summary>Identifier of the media asset to remove.</summary>
    public required Guid MediaId { get; init; }
}
