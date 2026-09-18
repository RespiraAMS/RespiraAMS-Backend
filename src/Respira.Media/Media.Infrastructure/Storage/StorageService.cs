using Amazon.S3;
using Amazon.S3.Model;
using Media.Application.Constracts.Storage;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Media.Infrastructure.Storage
{
    public sealed class StorageService : IStorageService
    {
        private readonly IAmazonS3 _s3Client;
        private readonly R2Options _options;
        private readonly ILogger<StorageService> _logger;

        public StorageService(
            IAmazonS3 s3Client,
            IOptions<R2Options> options,
            ILogger<StorageService> logger
        )
        {
            _s3Client = s3Client;
            _options = options.Value;
            _logger = logger;
        }

        public async Task<string> UploadAsync(
            UploadRequest request,
            CancellationToken cancellationToken = default
        )
        {
            var objectKey = BuildObjectKey(request.Folder, request.FileName);

            var putRequest = new PutObjectRequest
            {
                BucketName = _options.BucketName,
                Key = objectKey,
                InputStream = request.Content,
                ContentType = request.ContentType,
            };

            var response = await _s3Client.PutObjectAsync(putRequest, cancellationToken);

            _logger.LogInformation(
                "Uploaded {FileName} to R2 as {ObjectKey} (Status: {StatusCode})",
                request.FileName,
                objectKey,
                response.HttpStatusCode
            );

            return objectKey;
        }

        public async Task DeleteAsync(string objectKey, CancellationToken cancellationToken = default)
        {
            var deleteRequest = new DeleteObjectRequest
            {
                BucketName = _options.BucketName,
                Key = objectKey,
            };

            var response = await _s3Client.DeleteObjectAsync(deleteRequest, cancellationToken);

            _logger.LogInformation(
                "Deleted {ObjectKey} from R2 (Status: {StatusCode})",
                objectKey,
                response.HttpStatusCode
            );
        }

        public async Task<string> GetPresignedUrlAsync(
            string objectKey,
            TimeSpan? expiration = null,
            CancellationToken cancellationToken = default
        )
        {
            var request = new GetPreSignedUrlRequest
            {
                BucketName = _options.BucketName,
                Key = objectKey,
                Expires = DateTime.UtcNow.Add(expiration ?? TimeSpan.FromMinutes(15)),
            };

            var url = await _s3Client.GetPreSignedURLAsync(request);

            _logger.LogDebug("Generated pre-signed URL for {ObjectKey}", objectKey);

            return url;
        }

        private static string BuildObjectKey(string? folder, string fileName)
        {
            var sanitizedName = Path.GetFileName(fileName).Replace(' ', '_');
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            return string.IsNullOrEmpty(folder)
                ? $"{timestamp}_{sanitizedName}"
                : $"{folder.TrimEnd('/')}/{timestamp}_{sanitizedName}";
        }
    }
}
