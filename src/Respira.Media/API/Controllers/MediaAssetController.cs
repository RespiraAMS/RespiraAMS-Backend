using Application.Abstracts.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Respira.ServiceDefaults.Dtos;

namespace Respira.Media.API.Controllers;

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

public record MediaAssetDto
{
    public Guid Id { get; init; }
    public required string FileName { get; init; }
    public string? Url { get; init; }
    public string? ContentType { get; init; }
    public long? Size { get; init; }
}
