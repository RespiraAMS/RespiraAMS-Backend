using Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace Application.Contracts.Data;

public interface IDbContext
{
    DbSet<Patient> Patients { get; set; }
    DbSet<Treatment> Treatments { get; set; }

    /// <summary>
    /// Save all changes to database
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The number of records changed</returns>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}