using Microsoft.EntityFrameworkCore;
using Respira.Analysis.Domain.Entities;
using Respira.Analysis.Infrastructure.Data;
using Respira.ServiceDefaults.Auditing;

namespace Respira.Analysis.Infrastructure.Test.Data;

public class AnalysisDbContextFactoryTest
{
    [Fact]
    public void CreateDbContext_ReturnsConfiguredAnalysisDbContext()
    {
        var factory = new AnalysisDbContextFactory();

        using var context = factory.CreateDbContext([]);

        Assert.NotNull(context);
        Assert.IsType<AnalysisDbContext>(context);
        Assert.IsAssignableFrom<AuditableDbContext>(context);
        Assert.Contains(
            "Npgsql",
            context.Database.ProviderName ?? string.Empty,
            StringComparison.OrdinalIgnoreCase
        );

        var entityType = context.Model.FindEntityType(typeof(ClinicalAnalysis));
        Assert.NotNull(entityType);
        Assert.Equal("clinical_analyses", entityType!.GetTableName());
    }
}
