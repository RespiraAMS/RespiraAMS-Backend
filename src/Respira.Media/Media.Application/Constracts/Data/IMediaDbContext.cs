using Microsoft.EntityFrameworkCore;
using Respira.Media.Domain.Entities;

namespace Respira.Media.Application.Constracts.Data
{
    public interface IMediaDbContext
    {
        public DbSet<MediaAsset> MediaAssets { get; }
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
