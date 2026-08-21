using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Application.Abstracts.Data
{
    /// <summary>
    /// Persistence abstraction for the Doctor database, exposing the entity sets
    /// and a save method without leaking EF Core specifics to the Application layer.
    /// </summary>
    public interface IDoctorDbContext
    {
        /// <summary>Registered doctors</summary>
        public DbSet<Doctor> Doctors { get; set; }

        /// <summary>Persists pending changes to the database</summary>
        /// <returns>Number of affected rows</returns>
        public Task<int> SaveChangesAsync();
    }
}
