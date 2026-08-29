using Application.Abstracts.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Respira.ServiceDefaults.Constracts.CQRS;
using Respira.ServiceDefaults.Dtos;

namespace Application.Features.Media.Get.Queries
{
    public class GetMediaQueryHandler(
        IMediaDbContext dbContext,
        ILogger<GetMediaQueryHandler> logger
    ) : IQueryHandler<GetMediaQuery, ApiResponse<GetMediaResult>>
    {
        public async Task<ApiResponse<GetMediaResult>> HandleAsync(
            GetMediaQuery query,
            CancellationToken cancellationToken = default
        )
        {
            try
            {
                var media = await dbContext
                    .MediaAssets.AsNoTracking()
                    .FirstOrDefaultAsync(x => x.Id == query.Id, cancellationToken);

                if (media is null)
                {
                    return ApiResponse<GetMediaResult>.Fail(
                        "Media not found",
                        StatusCodes.Status404NotFound
                    );
                }

                return ApiResponse<GetMediaResult>.Ok(
                    new GetMediaResult { Url = media.Url ?? string.Empty }
                );
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error retrieving media asset");
                return ApiResponse<GetMediaResult>.Fail(
                    "Error retrieving media asset"
                );
            }
        }
    }
}
