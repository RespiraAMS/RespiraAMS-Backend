using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Respira.Patient.Infrastructure.Data;

public sealed class PatientDbContextFactory : IDesignTimeDbContextFactory<PatientDbContext>
{
    public PatientDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<PatientDbContext>()
            .UseNpgsql("Host=localhost;Database=patient-db;")
            .Options;

        return new PatientDbContext(options);
    }
}
