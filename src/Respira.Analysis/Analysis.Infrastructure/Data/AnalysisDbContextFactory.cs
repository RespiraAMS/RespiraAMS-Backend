using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Respira.Analysis.Infrastructure.Data;

public sealed class AnalysisDbContextFactory : IDesignTimeDbContextFactory<AnalysisDbContext>
{
    public AnalysisDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<AnalysisDbContext>()
            .UseNpgsql("Host=localhost;Database=analysis-db;")
            .Options;

        return new AnalysisDbContext(options);
    }
}
