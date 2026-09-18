using Media.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Media.Application.Constracts.Data
{
    public interface IMediaDbContext
    {
        public DbSet<MediaAsset> MediaAssets { get; set; }
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
