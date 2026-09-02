using Application.Abstracts.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Respira.ServiceDefaults.Contracts.CQRS;
using Respira.ServiceDefaults.Dtos;

namespace Application.Features.Media.Get.Queries
{
    public class GetMediaQueryHandler(
        IMediaDbContext dbContext,
        ILogger<GetMediaQueryHandler> logger
    ) : IQueryHandler<GetMediaQuery, Result<GetMediaResult>>
    {
        public async Task<Result<GetMediaResult>> HandleAsync(
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
                    return Result<GetMediaResult>.Fail(
                        "Media not found",
                        StatusCodes.Status404NotFound
                    );
                }

                return Result<GetMediaResult>.Ok(
                    new GetMediaResult { Url = media.Url ?? string.Empty }
                );
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error retrieving media asset");
                return Result<GetMediaResult>.Fail(
                    "Error retrieving media asset"
                );
            }
        }
    }
}
