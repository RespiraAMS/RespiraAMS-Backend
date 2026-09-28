using Asp.Versioning;
using Media.Application.Features.MediaAssets.Create;
using Microsoft.AspNetCore.Mvc;
using Respira.ServiceDefaults.Contracts.Results;
using Wolverine;

namespace Media.API.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/media-assets")]
    [Tags("MediaAssets")]
    public class MediaAssetsController(IMessageBus bus) : ControllerBase
    {
        /// <summary>
        /// Uploads an image file to R2 storage and records it as a media asset.
        /// The file name and content type are derived from the stream itself.
        /// </summary>
        [HttpPost]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(typeof(Result<bool>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(Result<bool>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Create(
            [FromForm] IFormFile file,
            [FromQuery] string? folder,
            CancellationToken cancellationToken
        )
        {
            if (file is null || file.Length == 0)
            {
                return BadRequest(
                    Result<bool>.Failure(
                        new Error(ApplicationStatus.BadRequest, "A file is required")
                    )
                );
            }

            await using var content = file.OpenReadStream();

            var command = new CreateMediaAssetCommand
            {
                MediaFile = content,
                Folder = folder,
            };

            var result = await bus.InvokeAsync<Result<bool>>(command, cancellationToken);

            return result.ToApiResponse();
        }
    }
}
