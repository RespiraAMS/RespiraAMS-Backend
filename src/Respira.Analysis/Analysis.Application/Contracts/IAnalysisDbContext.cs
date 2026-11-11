using Microsoft.EntityFrameworkCore;
using Respira.Analysis.Domain.Entities;

namespace Respira.Analysis.Application.Contracts
{
    public interface IAnalysisDbContext
    {
        DbSet<ClinicalAnalysis> ClinicalAnalyses { get; set; }
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
