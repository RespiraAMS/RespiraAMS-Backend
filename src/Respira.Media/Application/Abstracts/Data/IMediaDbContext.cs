using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Application.Abstracts.Data
{
    /// <summary>
    /// Persistence abstraction for the Media database.
    /// </summary>
    public interface IMediaDbContext
    {
        /// <summary>Set of uploaded media assets tracked by the Media database.</summary>
        DbSet<MediaAsset> MediaAssets { get; set; }

        /// <summary>
        /// Persists pending changes to the Media database.
        /// </summary>
        /// <param name="cancellationToken">Token to cancel the save operation.</param>
        /// <returns>The number of state entries written to the database.</returns>
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
