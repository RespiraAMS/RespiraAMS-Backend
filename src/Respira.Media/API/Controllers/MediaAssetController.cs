using Application.Abstracts.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Respira.ServiceDefaults.Dtos;

namespace Respira.Media.API.Controllers;

/// <summary>
/// Internal read endpoint that resolves a media asset by id, allowing other
/// Respira services (e.g. sagas) to fetch asset metadata without going through the
/// upload pipeline.
/// </summary>
[ApiController]
[Route("api/v1/media")]
public class MediaAssetController(IMediaDbContext dbContext) : ControllerBase
{
    /// <summary>
    /// Gets a media asset by ID (internal use by other services).
    /// </summary>
    [HttpGet]
    [Route("{id:guid}")]
    [ProducesResponseType<ApiResponse<MediaAssetDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiResponse>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var asset = await dbContext.MediaAssets
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (asset is null)
        {
            return NotFound(ApiResponse.Fail("Media asset not found", StatusCodes.Status404NotFound));
        }

        var dto = new MediaAssetDto
        {
            Id = asset.Id,
            FileName = asset.FileName,
            Url = asset.Url,
            ContentType = asset.ContentType,
            Size = asset.Size,
        };

        return Ok(ApiResponse<MediaAssetDto>.Ok(dto));
    }
}

/// <summary>
/// Lightweight projection of a <see cref="Domain.Entities.MediaAsset"/> returned to
/// internal callers by the media read endpoint.
/// </summary>
public record MediaAssetDto
{
    /// <summary>Unique identifier of the media asset.</summary>
    public Guid Id { get; init; }

    /// <summary>Original file name of the uploaded asset.</summary>
    public required string FileName { get; init; }

    /// <summary>Accessible URL of the stored object, if available.</summary>
    public string? Url { get; init; }

    /// <summary>MIME content type of the asset (e.g. image/png).</summary>
    public string? ContentType { get; init; }

    /// <summary>Size of the asset in bytes, if known.</summary>
    public long? Size { get; init; }
}
