using Application.Abstracts.Storage;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Infrastructure.Storage;

/// <summary>
/// Local-disk storage used for development when Cloudflare R2 is not configured
/// (i.e. <c>R2:Endpoint</c> is empty). Writes files under a local folder and
/// returns a <c>file://</c> URL so the rest of the saga flow can be exercised
/// without external object storage.
/// </summary>
public class LocalStorageService(IOptions<R2Options> options, IHostEnvironment environment)
    : IStorageService
{
    private readonly R2Options _options = options.Value;

    private string Root =>
        Path.Combine(environment.ContentRootPath, "local-media");

    public Task<StorageResult> UploadAsync(
        string fileName,
        string contentType,
        byte[] data,
        CancellationToken cancellationToken = default
    )
    {
        return UploadAsync(fileName, contentType, new MemoryStream(data), cancellationToken);
    }

    public async Task<StorageResult> UploadAsync(
        string fileName,
        string contentType,
        Stream stream,
        CancellationToken cancellationToken = default
    )
    {
        Directory.CreateDirectory(Root);

        var safeName = string.Concat(
            (fileName ?? "file").Where(c => !Path.GetInvalidFileNameChars().Contains(c))
        ).Replace(' ', '_');
        var objectKey = $"{Guid.CreateVersion7()}-{safeName}";
        var fullPath = Path.Combine(Root, objectKey);

        await using (var fs = File.Create(fullPath))
        {
            await stream.CopyToAsync(fs, cancellationToken);
        }

        var url = string.IsNullOrWhiteSpace(_options.PublicBaseUrl)
            ? $"file://{fullPath}"
            : $"{_options.PublicBaseUrl.TrimEnd('/')}/{objectKey}";

        return new StorageResult
        {
            ObjectKey = objectKey,
            Url = url,
            BucketName = _options.BucketName is { Length: > 0 } ? _options.BucketName : "local",
        };
    }

    public Task DeleteAsync(string objectKey, CancellationToken cancellationToken = default)
    {
        var fullPath = Path.Combine(Root, objectKey);
        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }

        return Task.CompletedTask;
    }
}
