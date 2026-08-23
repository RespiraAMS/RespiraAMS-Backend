using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Application.Abstracts.Data
{
    /// <summary>
    /// Persistence abstraction for the Media database.
    /// </summary>
    public interface IMediaDbContext
    {
        DbSet<MediaAsset> MediaAssets { get; set; }
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
