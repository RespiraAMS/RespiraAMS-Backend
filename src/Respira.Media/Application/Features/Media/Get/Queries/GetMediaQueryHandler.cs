using Application.Abstracts.Data;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Respira.ServiceDefaults.Constracts.CQRS;
using Respira.ServiceDefaults.Exceptions;
using Respira.ServiceDefaults.Messages;

namespace Application.Features.Media.Get.Queries
{
    public class GetMediaQueryHandler(
        IMediaDbContext dbContext,
        ILogger<GetMediaQueryHandler> logger
    ) : IQueryHandler<GetMediaQuery, GetMediaResult>
    {
        public async Task<GetMediaResult> HandleAsync(
            GetMediaQuery query,
            CancellationToken cancellationToken = default
        )
        {
            try
            {
                var media = await dbContext
                    .MediaAssets.AsNoTracking()
                    .FirstOrDefaultAsync(x => x.Id == query.Id, cancellationToken);

                return new GetMediaResult
                {
                    Url = media?.Url ?? string.Empty,
                };
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error retrieving media asset");
                throw new ServerException(ex);
            }
        }
    }
}
