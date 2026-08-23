using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Respira.SagaAudit.Application.Abstracts.Data;

/// <summary>
/// Persistence abstraction for the SagaAudit database.
/// </summary>
public interface ISagaAuditDbContext
{
    DbSet<ProcessTracker> ProcessTrackers { get; set; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
