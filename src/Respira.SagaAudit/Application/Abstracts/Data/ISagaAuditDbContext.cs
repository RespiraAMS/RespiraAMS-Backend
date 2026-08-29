using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Application.Abstracts.Data;

/// <summary>
/// Persistence abstraction for the SagaAudit database.
/// </summary>
public interface ISagaAuditDbContext
{
    /// <summary>Process tracker rows recording saga execution progress and status.</summary>
    DbSet<ProcessTracker> ProcessTrackers { get; set; }
    DbSet<Audit> Audits { get; set; }

    /// <summary>Persists pending changes to the SagaAudit database.</summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The number of rows affected.</returns>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
