using Cloudflare.NET.R2;
using Application.Abstracts.Storage;
using Microsoft.Extensions.Options;

namespace Infrastructure.Storage;

/// <summary>
/// Stores objects in a Cloudflare R2 bucket using the <c>Cloudflare.NET.R2</c> client,
/// which wraps the S3-compatible R2 API (intelligent uploads, payload-signing handling, retries).
/// </summary>
public class CloudflareR2StorageService(IR2Client r2Client, IOptions<R2Options> options) : IStorageService
{
    private readonly R2Options _options = options.Value;

    public async Task<StorageResult> UploadAsync(
        string fileName,
        string contentType,
        byte[] data,
        CancellationToken cancellationToken = default
    )
    {
        await using var stream = new MemoryStream(data);
        return await UploadAsync(fileName, contentType, stream, cancellationToken);
    }

    public async Task<StorageResult> UploadAsync(
        string fileName,
        string contentType,
        Stream stream,
        CancellationToken cancellationToken = default
    )
    {
        var objectKey = $"{Guid.CreateVersion7()}-{Sanitize(fileName)}";

        // UploadSinglePartAsync works with arbitrary (including non-seekable, e.g. HTTP upload)
        // streams and issues a single PUT, matching the previous behaviour. The library picks
        // multipart automatically when given a seekable stream above its size threshold.
        await r2Client.UploadSinglePartAsync(_options.BucketName, objectKey, stream, cancellationToken);

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
        await r2Client.DeleteObjectAsync(_options.BucketName, objectKey, cancellationToken);
    }

    private static string Sanitize(string fileName) =>
        string.Concat((fileName ?? "file").Where(c => !Path.GetInvalidFileNameChars().Contains(c)))
            .Replace(' ', '_');
}
