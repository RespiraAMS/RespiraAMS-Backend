using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Respira.Clinical.Infrastructure.Data;

public sealed class ClinicalDbContextFactory : IDesignTimeDbContextFactory<ClinicalDbContext>
{
    public ClinicalDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<ClinicalDbContext>()
            .UseNpgsql("Host=localhost;Database=clinical-db;")
            .Options;

        return new ClinicalDbContext(options);
    }
}
