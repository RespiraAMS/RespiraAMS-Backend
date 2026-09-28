using Media.Application.Constracts.Data;
using Media.Application.Constracts.Storage;
using Media.Application.Validators;
using Media.Domain.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Respira.ServiceDefaults.Contracts.CQRS;
using Respira.ServiceDefaults.Contracts.Results;

namespace Media.Application.Features.MediaAssets.Create
{
    public class CreateMediaAssetCommandHandler(
        IMediaDbContext dbContext,
        IStorageService storageService,
        IOptions<R2Options> r2Options,
        ILogger<CreateMediaAssetCommandHandler> logger
    ) : ICommandHandler<CreateMediaAssetCommand, Result<bool>>
    {
        public async Task<Result<bool>> HandleAsync(
            CreateMediaAssetCommand command,
            CancellationToken cancellationToken = default
        )
        {
            if (command.MediaFile is null)
            {
                return Failure(ApplicationStatus.BadRequest, "A file is required");
            }

            // The stream must be seekable to sniff its content and to rewind it before upload.
            var content = command.MediaFile;

            if (!content.CanSeek)
            {
                var buffered = new MemoryStream();
                await content.CopyToAsync(buffered, cancellationToken);
                buffered.Position = 0;
                content = buffered;
            }

            content.Position = 0;
            var size = content.Length;

            if (size == 0)
            {
                return Failure(ApplicationStatus.BadRequest, "The uploaded file is empty");
            }

            // 1. Determine the content type from the actual file bytes - the caller's
            //    reported content type is never trusted.
            var contentType = await ImageFileValidator.DetectContentTypeAsync(
                content,
                cancellationToken
            );

            if (contentType is null)
            {
                logger.LogWarning(
                    "Rejected upload: file content does not match any supported image format"
                );

                return Failure(
                    ApplicationStatus.BadRequest,
                    $"The file content is not a valid image. Supported formats: {ImageFileValidator.AllowedContentTypesText}"
                );
            }

            if (
                !string.IsNullOrWhiteSpace(command.ContentType)
                && !string.Equals(
                    command.ContentType.Trim(),
                    contentType,
                    StringComparison.OrdinalIgnoreCase
                )
            )
            {
                logger.LogWarning(
                    "Reported content type {ReportedContentType} differs from detected {DetectedContentType}, using detected",
                    command.ContentType,
                    contentType
                );
            }

            // 2. Resolve the file name: use the provided one, otherwise generate a
            //    unique name derived from the detected content type.
            var fileName = !string.IsNullOrWhiteSpace(command.FileName)
                ? Path.GetFileName(command.FileName.Trim())
                : GenerateFileName(contentType);

            if (!ImageFileValidator.IsAllowedExtension(fileName))
            {
                return Failure(
                    ApplicationStatus.BadRequest,
                    $"Only image files are allowed. Accepted extensions: {ImageFileValidator.AllowedExtensionsText}"
                );
            }

            // 3. Push the file to Cloudflare R2.
            content.Position = 0;

            var objectKey = await storageService.UploadAsync(
                new UploadRequest
                {
                    FileName = fileName,
                    Content = content,
                    ContentType = contentType,
                    Folder = command.Folder,
                },
                cancellationToken
            );

            // 4. Persist the MediaAsset record.
            var r2 = r2Options.Value;

            var mediaAsset = new MediaAsset
            {
                FileName = fileName,
                ObjectKey = objectKey,
                BucketName = r2.BucketName,
                ContentType = contentType,
                Size = size,
                Url = BuildPublicUrl(r2.PublicUrl, objectKey),
            };

            dbContext.MediaAssets.Add(mediaAsset);

            try
            {
                await dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (Exception exception)
            {
                // Compensate: remove the orphaned object when the database write fails.
                logger.LogError(
                    exception,
                    "Failed to persist MediaAsset for {ObjectKey}, rolling back R2 upload",
                    objectKey
                );

                try
                {
                    await storageService.DeleteAsync(objectKey, CancellationToken.None);
                }
                catch (Exception cleanupException)
                {
                    logger.LogWarning(
                        cleanupException,
                        "Failed to delete orphaned object {ObjectKey} from R2",
                        objectKey
                    );
                }

                throw;
            }

            logger.LogInformation(
                "Created MediaAsset {MediaAssetId} for {FileName} (stored as {ObjectKey})",
                mediaAsset.Id,
                fileName,
                objectKey
            );

            return Result<bool>.Success(ApplicationStatus.Created, true);
        }

        private static string GenerateFileName(string contentType)
        {
            var extension = ImageFileValidator.GetExtension(contentType);

            return $"media_{Guid.CreateVersion7():N}{extension}";
        }

        private static Result<bool> Failure(string statusCode, string description)
        {
            return Result<bool>.Failure(new Error(statusCode, description));
        }

        private static string? BuildPublicUrl(string? publicUrl, string objectKey)
        {
            return string.IsNullOrWhiteSpace(publicUrl)
                ? null
                : $"{publicUrl.TrimEnd('/')}/{objectKey}";
        }
    }
}
