namespace Application.Features.Media.Get.Queries;

/// <summary>
/// ApiResponse of a <see cref="GetMediaQuery"/>: the URL of a media asset.
/// </summary>
public record GetMediaResult
{
    public required string Url { get; set; }
}
