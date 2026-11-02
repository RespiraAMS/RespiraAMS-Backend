using Microsoft.EntityFrameworkCore;
using Respira.Media.Application.Contracts.Data;
using Respira.ServiceDefaults.Contracts.CQRS;
using Respira.ServiceDefaults.Contracts.Messages.Media;
using Respira.ServiceDefaults.Contracts.Results;

namespace Respira.Media.Application.Features.MediaAssets.GetByIds
{
    /// <summary>
    /// Answers media asset lookups sent by other microservices over the message broker.
    /// </summary>
    public class GetMediaAssetsByIdsQueryHandler(IMediaDbContext dbContext)
        : IQueryHandler<GetMediaAssetsByIdsQuery, Result<List<MediaAssetSummary>>>
    {
        public async Task<Result<List<MediaAssetSummary>>> HandleAsync(
            GetMediaAssetsByIdsQuery query,
            CancellationToken cancellationToken = default
        )
        {
            ArgumentNullException.ThrowIfNull(query);

            if (query.MediaIds.Length == 0)
            {
                return Result<List<MediaAssetSummary>>.Success(ApplicationStatus.Success, []);
            }

            var assets = await dbContext
                .MediaAssets.AsNoTracking()
                .Where(x => query.MediaIds.Contains(x.Id))
                .Select(x => new MediaAssetSummary { Id = x.Id, Url = x.Url })
                .ToListAsync(cancellationToken);

            return Result<List<MediaAssetSummary>>.Success(ApplicationStatus.Success, assets);
        }
    }
}
