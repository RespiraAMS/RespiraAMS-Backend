using Application.Abstracts.Data;
using Application.Abstracts.Storage;
using Application.Features.Media.Update.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Respira.ServiceDefaults.Constracts.CQRS;
using Wolverine;

namespace Application.Features.Media.Update.Commands;

public class UpdateMediaCommandHandler(
    ILogger<UpdateMediaCommand> logger,
    IMediaDbContext dbContext,
    IStorageService storageService,
    IMessageBus bus
) : ICommandHandler<UpdateMediaCommand>
{
    public async Task HandleAsync(
        UpdateMediaCommand command,
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
                logger.LogWarning("Media {MediaId} not found for update", command.MediaId);
                await bus.PublishAsync(
                    new UpdateMediaFailure
                    {
                        SagaId = command.SagaId,
                        MediaId = command.MediaId,
                        Message = "Media not found",
                    }
                );
                return;
            }

            var previousObjectKey = asset.ObjectKey;

            var result = await storageService.UploadAsync(
                command.FileName,
                command.ContentType,
                command.Data,
                cancellationToken
            );

            asset.FileName = command.FileName;
            asset.ContentType = command.ContentType;
            asset.Size = command.Size;
            asset.ObjectKey = result.ObjectKey;
            asset.BucketName = result.BucketName;
            asset.Url = result.Url;
            asset.UpdatedAt = DateTimeOffset.UtcNow;

            if (!string.IsNullOrWhiteSpace(previousObjectKey))
            {
                await storageService.DeleteAsync(previousObjectKey, cancellationToken);
            }

            dbContext.MediaAssets.Update(asset);
            await dbContext.SaveChangesAsync(cancellationToken);

            await bus.PublishAsync(
                new UpdateMediaSuccess
                {
                    SagaId = command.SagaId,
                    MediaId = asset.Id,
                    Url = asset.Url,
                }
            );
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to update media");
            await bus.PublishAsync(
                new UpdateMediaFailure
                {
                    SagaId = command.SagaId,
                    MediaId = command.MediaId,
                    Message = "Failed to update media",
                }
            );
        }
    }
}
