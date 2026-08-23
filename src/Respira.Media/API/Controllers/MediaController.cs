using Application.Abstracts.Data;
using Application.Abstracts.Storage;
using Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Respira.Media.API.Controllers;

/// <summary>
/// Streaming upload endpoint for media assets. The caller uploads the file directly here
/// (multipart/form-data) and receives a <c>mediaId</c> that subsequent sagas reference, so the
/// raw bytes never travel through the Wolverine/RabbitMQ message bus.
/// </summary>
[ApiController]
[Route("api/v1/media")]
public class MediaController(
    IStorageService storageService,
    IMediaDbContext dbContext
) : ControllerBase
{
    private const long MaxFileSize = 10 * 1024 * 1024; // 10 MB

    [HttpPost("upload")]
    [RequestSizeLimit(MaxFileSize)]
    public async Task<IActionResult> Upload([FromForm] IFormFile file, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest("File content is required");
        }

        if (file.Length > MaxFileSize)
        {
            return BadRequest($"File size must not exceed {MaxFileSize / (1024 * 1024)} MB");
        }

        if (string.IsNullOrWhiteSpace(file.ContentType)
            || !file.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest("Only image files are allowed");
        }

        await using var stream = file.OpenReadStream();
        var result = await storageService.UploadAsync(
            file.FileName,
            file.ContentType,
            stream,
            cancellationToken
        );

        var asset = new MediaAsset
        {
            Id = Guid.CreateVersion7(),
            FileName = file.FileName,
            ContentType = file.ContentType,
            Size = file.Length,
            ObjectKey = result.ObjectKey,
            BucketName = result.BucketName,
            Url = result.Url,
        };

        dbContext.MediaAssets.Add(asset);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok(new { mediaId = asset.Id, url = asset.Url });
    }
}
