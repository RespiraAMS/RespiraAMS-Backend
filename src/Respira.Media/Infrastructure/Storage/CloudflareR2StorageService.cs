using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using Application.Abstracts.Storage;
using Microsoft.Extensions.Options;

namespace Infrastructure.Storage;

/// <summary>
/// Stores objects in a Cloudflare R2 bucket (S3-compatible API).
/// </summary>
public class CloudflareR2StorageService(IOptions<R2Options> options) : IStorageService
{
    private readonly R2Options _options = options.Value;

    public async Task<StorageResult> UploadAsync(
        string fileName,
        string contentType,
        byte[] data,
        CancellationToken cancellationToken = default
    )
    {
        var config = BuildConfig();
        using var client = new AmazonS3Client(_options.AccessKey, _options.SecretKey, config);

        var objectKey = $"{Guid.CreateVersion7()}-{Sanitize(fileName)}";
        await using var stream = new MemoryStream(data);
        var request = new PutObjectRequest
        {
            BucketName = _options.BucketName,
            Key = objectKey,
            InputStream = stream,
            ContentType = contentType,
        };

        await client.PutObjectAsync(request, cancellationToken);

        var url = string.IsNullOrWhiteSpace(_options.PublicBaseUrl)
            ? $"{_options.Endpoint.TrimEnd('/')}/{_options.BucketName}/{objectKey}"
            : $"{_options.PublicBaseUrl.TrimEnd('/')}/{objectKey}";

        return new StorageResult
        {
            ObjectKey = objectKey,
            Url = url,
            BucketName = _options.BucketName,
        };
    }

    public async Task DeleteAsync(string objectKey, CancellationToken cancellationToken = default)
    {
        var config = BuildConfig();
        using var client = new AmazonS3Client(_options.AccessKey, _options.SecretKey, config);
        await client.DeleteObjectAsync(_options.BucketName, objectKey, cancellationToken);
    }

    private AmazonS3Config BuildConfig() =>
        new()
        {
            ServiceURL = _options.Endpoint,
            ForcePathStyle = true,
            RegionEndpoint = RegionEndpoint.USEast1,
        };

    private static string Sanitize(string fileName) =>
        string.Concat((fileName ?? "file").Where(c => !Path.GetInvalidFileNameChars().Contains(c)))
            .Replace(' ', '_');
}
