using Microsoft.EntityFrameworkCore;
using Respira.Saga.Domain.Entities;

namespace Respira.Saga.Application.Contracts.Data
{
    public interface ISagaAuditDbContext
    {
        DbSet<AuditLogEntry> AuditLogs { get; }
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
