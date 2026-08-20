using Infrastructure.Persistence.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Infrastructure;

/// <summary>
/// Design-time factory used by the EF Core tooling to build the DoctorDbContext
/// when generating migrations. The connection string here is only used at design
/// time to build the model; at runtime the context is configured via Aspire.
/// </summary>
public class DesignTimeDoctorDbContextFactory : IDesignTimeDbContextFactory<DoctorDbContext>
{
    /// <summary>
    /// Creates a DoctorDbContext instance for the EF Core tooling
    /// </summary>
    /// <param name="args">Command-line arguments from the EF Core tools</param>
    /// <returns>A configured DoctorDbContext</returns>
    public DoctorDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<DoctorDbContext>();
        optionsBuilder.UseNpgsql(
            "Host=localhost;Port=5432;Database=doctor_db;Username=postgres;Password=postgres"
        );
        return new DoctorDbContext(optionsBuilder.Options);
    }
}
