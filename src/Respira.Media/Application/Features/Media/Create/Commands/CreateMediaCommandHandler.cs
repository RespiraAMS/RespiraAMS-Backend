using Application.Abstracts.Data;
using Application.Abstracts.Storage;
using Application.Features.Media.Create.Events;
using Microsoft.Extensions.Logging;
using Respira.ServiceDefaults.Constracts.CQRS;
using Wolverine;

namespace Application.Features.Media.Create.Commands;

public class CreateMediaCommandHandler(
    ILogger<CreateMediaCommand> logger,
    IMediaDbContext dbContext,
    IStorageService storageService,
    IMessageBus bus
) : ICommandHandler<CreateMediaCommand>
{
    public async Task HandleAsync(
        CreateMediaCommand command,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            var asset = new Domain.Entities.MediaAsset
            {
                Id = command.MediaId,
                FileName = command.FileName,
                ContentType = command.ContentType,
                Size = command.Size,
            };

            var result = await storageService.UploadAsync(
                command.FileName,
                command.ContentType,
                command.Data,
                cancellationToken
            );

            asset.ObjectKey = result.ObjectKey;
            asset.BucketName = result.BucketName;
            asset.Url = result.Url;

            dbContext.MediaAssets.Add(asset);
            await dbContext.SaveChangesAsync(cancellationToken);

            await bus.PublishAsync(
                new CreateMediaSuccess
                {
                    SagaId = command.SagaId,
                    MediaId = asset.Id,
                    Url = asset.Url,
                }
            );
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to create media");
            await bus.PublishAsync(
                new CreateMediaFailure
                {
                    SagaId = command.SagaId,
                    Message = "Failed to create media",
                }
            );
        }
    }
}
