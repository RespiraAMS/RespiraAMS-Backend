using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Respira.Analysis.Domain.Entities;
using Respira.Analysis.Infrastructure.Data;
using Respira.ServiceDefaults.Auditing;

namespace Respira.Analysis.Infrastructure.Test.Data;

public sealed class AnalysisDbContextTest : IClassFixture<PostgresFixture>, IAsyncLifetime
{
    private readonly PostgresFixture _fixture;
    private readonly DbContextOptions<AnalysisDbContext> _options;
    private readonly AnalysisDbContext _context;

    public AnalysisDbContextTest(PostgresFixture fixture)
    {
        _fixture = fixture;

        _options = new DbContextOptionsBuilder<AnalysisDbContext>()
            .UseNpgsql(fixture.ConnectionString)
            .Options;

        _context = new AnalysisDbContext(_options);
    }

    public async ValueTask InitializeAsync()
    {
        await using var context = new AnalysisDbContext(_options);
        await context.AuditEntries.ExecuteDeleteAsync(TestContext.Current.CancellationToken);
        await context
            .ClinicalAnalyses.IgnoreQueryFilters()
            .ExecuteDeleteAsync(TestContext.Current.CancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        await _context.DisposeAsync();
    }

    [Fact]
    public void Context_IsAnAuditableDbContext()
    {
        Assert.IsAssignableFrom<AuditableDbContext>(_context);
        Assert.IsType<AnalysisDbContext>(_context);
    }

    [Fact]
    public async Task Context_ExposesTheSharedAuditEntriesTable()
    {
        var entityType = _context.Model.FindEntityType(typeof(AuditEntry));

        Assert.NotNull(entityType);
        Assert.Equal("audit_entries", entityType!.GetTableName());

        await using var context = new AnalysisDbContext(_options);
        var tableCount = await context
            .Database.SqlQueryRaw<int>(
                "SELECT COUNT(*)::int AS \"Value\" FROM information_schema.tables WHERE table_name = 'audit_entries'"
            )
            .SingleAsync(TestContext.Current.CancellationToken);

        Assert.Equal(1, tableCount);
    }

    [Fact]
    public async Task SaveChanges_CreatingAnalysis_WritesACreatedAuditEntry()
    {
        var analysis = NewAnalysis();

        await using (var context = new AnalysisDbContext(_options))
        {
            context.ClinicalAnalyses.Add(analysis);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await using var verifyContext = new AnalysisDbContext(_options);
        var auditEntry = await verifyContext
            .AuditEntries.AsNoTracking()
            .SingleAsync(
                item => item.EntityId == analysis.Id,
                TestContext.Current.CancellationToken
            );

        Assert.Equal("Analysis.ClinicalAnalysis.Created", auditEntry.Action);
        Assert.Equal("Created", auditEntry.Operation);
    }

    [Fact]
    public async Task AuditEntry_CarriesTheEntityIdAndEntityType()
    {
        var analysis = NewAnalysis();

        await using (var context = new AnalysisDbContext(_options))
        {
            context.ClinicalAnalyses.Add(analysis);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await using var verifyContext = new AnalysisDbContext(_options);
        var auditEntry = await verifyContext
            .AuditEntries.AsNoTracking()
            .SingleAsync(
                item => item.EntityId == analysis.Id,
                TestContext.Current.CancellationToken
            );

        Assert.Equal(analysis.Id, auditEntry.EntityId);
        Assert.Equal(typeof(ClinicalAnalysis).FullName, auditEntry.EntityType);
        Assert.EndsWith("ClinicalAnalysis", auditEntry.EntityType);
    }

    [Fact]
    public async Task SaveChanges_UpdatingReport_WritesAnUpdatedAuditEntry()
    {
        var analysis = NewAnalysis();

        await using (var context = new AnalysisDbContext(_options))
        {
            context.ClinicalAnalyses.Add(analysis);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await using (var context = new AnalysisDbContext(_options))
        {
            var tracked = await context.ClinicalAnalyses.SingleAsync(
                item => item.Id == analysis.Id,
                TestContext.Current.CancellationToken
            );

            tracked.ReportJson = "{\"analysisId\":\"updated\"}";
            tracked.UpdatedAt = DateTimeOffset.UtcNow;
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await using var verifyContext = new AnalysisDbContext(_options);
        var auditEntry = await verifyContext
            .AuditEntries.AsNoTracking()
            .SingleAsync(
                item => item.EntityId == analysis.Id && item.Operation == "Updated",
                TestContext.Current.CancellationToken
            );

        Assert.Equal("Analysis.ClinicalAnalysis.Updated", auditEntry.Action);
        Assert.Equal("Updated", auditEntry.Operation);
    }

    [Fact]
    public async Task AuditEntry_ListsTheChangedFields()
    {
        var analysis = NewAnalysis();

        await using (var context = new AnalysisDbContext(_options))
        {
            context.ClinicalAnalyses.Add(analysis);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await using (var context = new AnalysisDbContext(_options))
        {
            var tracked = await context.ClinicalAnalyses.SingleAsync(
                item => item.Id == analysis.Id,
                TestContext.Current.CancellationToken
            );

            tracked.ReportJson = "{\"analysisId\":\"changed\"}";
            tracked.UpdatedAt = DateTimeOffset.UtcNow;
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await using var verifyContext = new AnalysisDbContext(_options);
        var auditEntry = await verifyContext
            .AuditEntries.AsNoTracking()
            .SingleAsync(
                item => item.EntityId == analysis.Id && item.Operation == "Updated",
                TestContext.Current.CancellationToken
            );

        var changedFields = JsonSerializer.Deserialize<string[]>(auditEntry.ChangedFieldsJson);

        Assert.NotNull(changedFields);
        Assert.Contains(nameof(ClinicalAnalysis.ReportJson), changedFields!);
        Assert.Contains(nameof(ClinicalAnalysis.UpdatedAt), changedFields!);
        Assert.DoesNotContain(nameof(ClinicalAnalysis.DoctorId), changedFields!);
    }

    [Fact]
    public async Task SaveChanges_SoftDeletingReport_WritesASoftDeletedAuditEntry()
    {
        var analysis = NewAnalysis();

        await using (var context = new AnalysisDbContext(_options))
        {
            context.ClinicalAnalyses.Add(analysis);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await using (var context = new AnalysisDbContext(_options))
        {
            var tracked = await context.ClinicalAnalyses.SingleAsync(
                item => item.Id == analysis.Id,
                TestContext.Current.CancellationToken
            );

            tracked.IsDeleted = true;
            tracked.DeletedAt = DateTimeOffset.UtcNow;
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await using var verifyContext = new AnalysisDbContext(_options);
        var auditEntry = await verifyContext
            .AuditEntries.AsNoTracking()
            .SingleAsync(
                item => item.EntityId == analysis.Id && item.Operation == "SoftDeleted",
                TestContext.Current.CancellationToken
            );

        Assert.Equal("Analysis.ClinicalAnalysis.SoftDeleted", auditEntry.Action);
        Assert.Equal("SoftDeleted", auditEntry.Operation);
    }

    [Fact]
    public async Task SaveChanges_HardDeletingReport_WritesADeletedAuditEntry()
    {
        var analysis = NewAnalysis();

        await using (var context = new AnalysisDbContext(_options))
        {
            context.ClinicalAnalyses.Add(analysis);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await using (var context = new AnalysisDbContext(_options))
        {
            var tracked = await context.ClinicalAnalyses.SingleAsync(
                item => item.Id == analysis.Id,
                TestContext.Current.CancellationToken
            );

            context.ClinicalAnalyses.Remove(tracked);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await using var verifyContext = new AnalysisDbContext(_options);
        var auditEntry = await verifyContext
            .AuditEntries.AsNoTracking()
            .SingleAsync(
                item => item.EntityId == analysis.Id && item.Operation == "Deleted",
                TestContext.Current.CancellationToken
            );

        Assert.Equal("Analysis.ClinicalAnalysis.Deleted", auditEntry.Action);
        Assert.Equal("Deleted", auditEntry.Operation);
    }

    [Fact]
    public async Task AuditEntriesOfOneSaveChangesCall_ShareTheSameChangeSetId()
    {
        var first = NewAnalysis();
        var second = NewAnalysis();

        await using (var context = new AnalysisDbContext(_options))
        {
            context.ClinicalAnalyses.AddRange(first, second);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await using var verifyContext = new AnalysisDbContext(_options);
        var changeSetIds = await verifyContext
            .AuditEntries.AsNoTracking()
            .Where(item => item.EntityId == first.Id || item.EntityId == second.Id)
            .Select(item => item.ChangeSetId)
            .ToListAsync(TestContext.Current.CancellationToken);

        Assert.Equal(2, changeSetIds.Count);
        Assert.Equal(changeSetIds[0], changeSetIds[1]);
    }

    [Fact]
    public async Task TwoSeparateSaveChangesCalls_ProduceDifferentChangeSetIds()
    {
        var first = NewAnalysis();
        var second = NewAnalysis();

        await using (var context = new AnalysisDbContext(_options))
        {
            context.ClinicalAnalyses.Add(first);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await using (var context = new AnalysisDbContext(_options))
        {
            context.ClinicalAnalyses.Add(second);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await using var verifyContext = new AnalysisDbContext(_options);
        var changeSetIds = await verifyContext
            .AuditEntries.AsNoTracking()
            .Where(item => item.EntityId == first.Id || item.EntityId == second.Id)
            .Select(item => item.ChangeSetId)
            .ToListAsync(TestContext.Current.CancellationToken);

        Assert.Equal(2, changeSetIds.Count);
        Assert.NotEqual(changeSetIds[0], changeSetIds[1]);
    }

    [Fact]
    public async Task JsonPayloadColumns_AreStoredAsJsonb()
    {
        var entityType = _context.Model.FindEntityType(typeof(ClinicalAnalysis));

        Assert.NotNull(entityType);
        Assert.Equal(
            "jsonb",
            entityType!.FindProperty(nameof(ClinicalAnalysis.PatientIdsJson))!.GetColumnType()
        );
        Assert.Equal(
            "jsonb",
            entityType.FindProperty(nameof(ClinicalAnalysis.TreatmentPlanIdsJson))!.GetColumnType()
        );
        Assert.Equal(
            "jsonb",
            entityType.FindProperty(nameof(ClinicalAnalysis.ReportJson))!.GetColumnType()
        );

        var analysis = NewAnalysis();

        await using (var context = new AnalysisDbContext(_options))
        {
            context.ClinicalAnalyses.Add(analysis);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await using var verifyContext = new AnalysisDbContext(_options);
        var jsonbColumnCount = await verifyContext
            .Database.SqlQueryRaw<int>(
                "SELECT COUNT(*)::int AS \"Value\" FROM information_schema.columns "
                    + "WHERE table_name = 'clinical_analyses' "
                    + "AND column_name IN ('PatientIdsJson', 'TreatmentPlanIdsJson', 'ReportJson') "
                    + "AND data_type = 'jsonb'"
            )
            .SingleAsync(TestContext.Current.CancellationToken);

        Assert.Equal(3, jsonbColumnCount);

        // The stored payload can be read back through a ::jsonb cast
        var storedJson = await verifyContext
            .Database.SqlQueryRaw<string>(
                "SELECT \"ReportJson\"::jsonb::text AS \"Value\" FROM clinical_analyses WHERE \"Id\" = {0}",
                analysis.Id
            )
            .SingleAsync(TestContext.Current.CancellationToken);

        Assert.False(string.IsNullOrWhiteSpace(storedJson));
        using var document = JsonDocument.Parse(storedJson!);
        Assert.Equal(JsonValueKind.Object, document.RootElement.ValueKind);
    }

    [Fact]
    public async Task CreatedAtIndex_Exists()
    {
        var entityType = _context.Model.FindEntityType(typeof(ClinicalAnalysis));

        Assert.NotNull(entityType);
        var index = entityType!
            .GetIndexes()
            .SingleOrDefault(item =>
                item.Properties.Count == 1
                && item.Properties[0].Name == nameof(ClinicalAnalysis.CreatedAt)
            );
        Assert.NotNull(index);

        await using var context = new AnalysisDbContext(_options);
        var indexCount = await context
            .Database.SqlQueryRaw<int>(
                "SELECT COUNT(*)::int AS \"Value\" FROM pg_indexes "
                    + "WHERE tablename = 'clinical_analyses' "
                    + "AND indexname = 'IX_clinical_analyses_CreatedAt'"
            )
            .SingleAsync(TestContext.Current.CancellationToken);

        Assert.Equal(1, indexCount);
    }

    [Fact]
    public void Context_CanBeResolvedFromScope_WithAuditLoggingAndValidateScopes()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAuditLogging();
        services.AddDbContext<AnalysisDbContext>(options =>
            options.UseNpgsql(_fixture.ConnectionString)
        );

        using var provider = services.BuildServiceProvider(
            new ServiceProviderOptions { ValidateScopes = true }
        );
        using var scope = provider.CreateScope();

        var context = scope.ServiceProvider.GetRequiredService<AnalysisDbContext>();
        var accessor = scope.ServiceProvider.GetRequiredService<IAuditContextAccessor>();

        Assert.NotNull(context);
        Assert.NotNull(accessor);
        Assert.IsType<AnalysisDbContext>(context);
    }

    private static ClinicalAnalysis NewAnalysis() =>
        new()
        {
            DoctorId = Guid.CreateVersion7(),
            Start = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
            End = new DateTimeOffset(2026, 2, 1, 0, 0, 0, TimeSpan.Zero),
            PatientIdsJson = "[\"10000000-0000-0000-0000-000000000001\"]",
            TreatmentPlanIdsJson = "[\"20000000-0000-0000-0000-000000000001\"]",
            ReportJson = "{}",
        };
}
