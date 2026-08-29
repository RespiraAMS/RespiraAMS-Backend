using Application.Abstracts.Data;
using Application.Abstracts.Storage;
using Application.Features.Media.Remove.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Respira.ServiceDefaults.Constracts.CQRS;
using Wolverine;

namespace Application.Features.Media.Remove.Commands;

/// <summary>
/// Handles <see cref="RemoveMediaCommand"/>. Soft-deletes the media asset, deletes its object
/// from storage, and publishes a <see cref="RemoveMediaSuccess"/> or <see cref="RemoveMediaFailure"/>
/// event so the originating saga can continue or compensate. If the asset is already missing, a
/// success is published to avoid blocking the saga.
/// </summary>
public class RemoveMediaCommandHandler(
    ILogger<RemoveMediaCommand> logger,
    IMediaDbContext dbContext,
    IStorageService storageService,
    IMessageBus bus
) : ICommandHandler<RemoveMediaCommand>
{
    /// <summary>
    /// Executes the removal: loads the asset, deletes the stored object, marks the asset as
    /// deleted, and publishes the outcome event.
    /// </summary>
    /// <param name="command">The remove command carrying the saga and media identifiers.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    public async Task HandleAsync(
        RemoveMediaCommand command,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            var asset = await dbContext.MediaAssets.FirstOrDefaultAsync(
                x => x.Id == command.MediaId,
                cancellationToken
            );
            if (asset is null)
            {
                logger.LogWarning("Media {MediaId} not found for remove", command.MediaId);
                await bus.PublishAsync(
                    new RemoveMediaSuccess
                    {
                        SagaId = command.SagaId,
                        MediaId = command.MediaId,
                    }
                );
                return;
            }

            if (!string.IsNullOrWhiteSpace(asset.ObjectKey))
            {
                await storageService.DeleteAsync(asset.ObjectKey, cancellationToken);
            }

            asset.IsDeleted = true;
            asset.DeletedAt = DateTimeOffset.UtcNow;
            asset.UpdatedAt = DateTimeOffset.UtcNow;
            dbContext.MediaAssets.Update(asset);
            await dbContext.SaveChangesAsync(cancellationToken);

            await bus.PublishAsync(
                new RemoveMediaSuccess
                {
                    SagaId = command.SagaId,
                    MediaId = asset.Id,
                }
            );
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to remove media");
            await bus.PublishAsync(
                new RemoveMediaFailure
                {
                    SagaId = command.SagaId,
                    MediaId = command.MediaId,
                    Message = "Failed to remove media",
                }
            );
        }
    }
}
