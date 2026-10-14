using Media.Application.Constracts.Data;
using Media.Application.Constracts.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Respira.ServiceDefaults.Contracts.CQRS;
using Respira.ServiceDefaults.Contracts.Results;

namespace Media.Application.Features.MediaAssets.Delete
{
    public class DeleteMediaAssetCommandHandler(
        IMediaDbContext dbContext,
        IStorageService storageService,
        ILogger<DeleteMediaAssetCommandHandler> logger
    ) : ICommandHandler<DeleteMediaAssetCommand, Result<Guid>>
    {
        public async Task<Result<Guid>> HandleAsync(
            DeleteMediaAssetCommand command,
            CancellationToken cancellationToken = default
        )
        {
            if (command.MediaId == Guid.Empty)
            {
                return Failure(ApplicationStatus.BadRequest, "A media id is required");
            }

            var asset = await dbContext.MediaAssets.FirstOrDefaultAsync(
                x => x.Id == command.MediaId,
                cancellationToken
            );

            if (asset is null)
            {
                // Idempotent: a delete that is retried after it already succeeded still
                // counts as a success, which is what lets the sagas be re-sent safely.
                logger.LogDebug(
                    "MediaAsset {MediaAssetId} already gone, treating delete as completed",
                    command.MediaId
                );

                return Result<Guid>.Success(ApplicationStatus.Deleted, command.MediaId);
            }

            if (!string.IsNullOrWhiteSpace(asset.ObjectKey))
            {
                try
                {
                    await storageService.DeleteAsync(asset.ObjectKey, cancellationToken);
                }
                catch (Exception exception)
                {
                    logger.LogError(
                        exception,
                        "Failed to delete object {ObjectKey} from storage",
                        asset.ObjectKey
                    );

                    // The record is kept so a retry can try the storage call again.
                    return Failure(
                        ApplicationStatus.ThirdPartyServiceFailure,
                        $"The asset could not be deleted from storage: {exception.Message}"
                    );
                }
            }

            dbContext.MediaAssets.Remove(asset);

            try
            {
                await dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (Exception exception)
            {
                logger.LogError(
                    exception,
                    "Failed to remove the record of MediaAsset {MediaAssetId}",
                    command.MediaId
                );

                // The object is already gone; a retry runs a storage delete on a key that
                // no longer exists (which S3 treats as a success) and finishes the removal.
                return Failure(
                    ApplicationStatus.ServerError,
                    $"The asset record could not be removed: {exception.Message}"
                );
            }

            logger.LogInformation("Deleted MediaAsset {MediaAssetId}", command.MediaId);

            return Result<Guid>.Success(ApplicationStatus.Deleted, command.MediaId);
        }

        private static Result<Guid> Failure(string statusCode, string description)
        {
            return Result<Guid>.Failure(new Error(statusCode, description));
        }
    }
}
