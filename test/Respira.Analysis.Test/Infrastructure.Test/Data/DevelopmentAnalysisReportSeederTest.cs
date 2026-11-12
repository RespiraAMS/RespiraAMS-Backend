using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Respira.Analysis.Application.Contracts;
using Respira.Analysis.Domain.Entities;
using Respira.Analysis.Infrastructure.Data;
using Respira.ServiceDefaults.Auditing;

namespace Respira.Analysis.Infrastructure.Test.Data;

public sealed class DevelopmentAnalysisReportSeederTest
    : IClassFixture<PostgresFixture>,
        IAsyncLifetime
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly PostgresFixture _fixture;
    private readonly DbContextOptions<AnalysisDbContext> _options;
    private readonly AnalysisDbContext _context;

    public DevelopmentAnalysisReportSeederTest(PostgresFixture fixture)
    {
        _fixture = fixture;

        _options = new DbContextOptionsBuilder<AnalysisDbContext>()
            .UseNpgsql(fixture.ConnectionString)
            .Options;

        _context = new AnalysisDbContext(_options);
    }

    public async ValueTask InitializeAsync()
    {
        await _context.AuditEntries.ExecuteDeleteAsync(TestContext.Current.CancellationToken);
        await _context
            .ClinicalAnalyses.IgnoreQueryFilters()
            .ExecuteDeleteAsync(TestContext.Current.CancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        await _context.DisposeAsync();
    }

    [Fact]
    public async Task SeedAnalysisReports_CreatesTheThreeReports()
    {
        await SeedAsync();

        await using var context = new AnalysisDbContext(_options);
        var reports = await context
            .ClinicalAnalyses.IgnoreQueryFilters()
            .AsNoTracking()
            .ToListAsync(TestContext.Current.CancellationToken);

        Assert.Equal(3, reports.Count);
        Assert.Contains(
            reports,
            item => item.Id == DevelopmentAnalysisReportSeeder.DoctorReportId
        );
        Assert.Contains(
            reports,
            item => item.Id == DevelopmentAnalysisReportSeeder.SystemReportId
        );
        Assert.Contains(
            reports,
            item => item.Id == DevelopmentAnalysisReportSeeder.SoftDeletedReportId
        );
    }

    [Fact]
    public async Task SeedAnalysisReports_DoctorReportBelongsToDoctorOne_AndSystemReportHasNoDoctor()
    {
        await SeedAsync();

        await using var context = new AnalysisDbContext(_options);
        var doctorReport = await LoadReportAsync(
            context,
            DevelopmentAnalysisReportSeeder.DoctorReportId
        );
        var systemReport = await LoadReportAsync(
            context,
            DevelopmentAnalysisReportSeeder.SystemReportId
        );

        Assert.NotNull(doctorReport);
        Assert.Equal(DevelopmentAnalysisReportSeeder.DoctorOneId, doctorReport!.DoctorId);
        Assert.NotNull(systemReport);
        Assert.Null(systemReport!.DoctorId);
    }

    [Fact]
    public async Task SeedAnalysisReports_SoftDeletedReportIsFlaggedAndTimestamped()
    {
        await SeedAsync();

        await using var context = new AnalysisDbContext(_options);
        var softDeleted = await LoadReportAsync(
            context,
            DevelopmentAnalysisReportSeeder.SoftDeletedReportId
        );

        Assert.NotNull(softDeleted);
        Assert.True(softDeleted!.IsDeleted);
        Assert.NotNull(softDeleted.DeletedAt);
    }

    [Fact]
    public async Task SeedAnalysisReports_SoftDeletedReportIsHiddenByTheNotDeletedFilter()
    {
        await SeedAsync();

        await using var context = new AnalysisDbContext(_options);

        // The query handlers filter soft deleted rows with `!analysis.IsDeleted`
        var visibleIds = await context
            .ClinicalAnalyses.AsNoTracking()
            .Where(analysis => !analysis.IsDeleted)
            .Select(analysis => analysis.Id)
            .ToListAsync(TestContext.Current.CancellationToken);

        Assert.DoesNotContain(DevelopmentAnalysisReportSeeder.SoftDeletedReportId, visibleIds);
        Assert.Contains(DevelopmentAnalysisReportSeeder.DoctorReportId, visibleIds);
        Assert.Contains(DevelopmentAnalysisReportSeeder.SystemReportId, visibleIds);

        // The row is still physically present, it is only hidden
        var allIds = await context
            .ClinicalAnalyses.IgnoreQueryFilters()
            .AsNoTracking()
            .Select(analysis => analysis.Id)
            .ToListAsync(TestContext.Current.CancellationToken);

        Assert.Contains(DevelopmentAnalysisReportSeeder.SoftDeletedReportId, allIds);
    }

    [Fact]
    public async Task SeedAnalysisReports_ReportJsonDeserialisesIntoAnalysisReportResult()
    {
        await SeedAsync();

        await using var context = new AnalysisDbContext(_options);
        var systemReport = await LoadReportAsync(
            context,
            DevelopmentAnalysisReportSeeder.SystemReportId
        );

        Assert.NotNull(systemReport);
        var result = JsonSerializer.Deserialize<AnalysisReportResult>(
            systemReport!.ReportJson,
            JsonOptions
        );

        Assert.NotNull(result);
        Assert.Equal(DevelopmentAnalysisReportSeeder.SystemReportId, result!.AnalysisId);
        Assert.Null(result.DoctorId);
        Assert.Equal(4, result.TotalPatients);
        Assert.Equal(5, result.TotalTreatmentPlans);
        Assert.Equal(2, result.TotalAdherentPlans);
        Assert.Equal(2, result.TotalNonAdherentPlans);
        Assert.Equal(1, result.TotalPendingReviewPlans);
    }

    [Fact]
    public async Task SeedAnalysisReports_DoctorReportCarriesTheDoctorOneSummary()
    {
        await SeedAsync();

        await using var context = new AnalysisDbContext(_options);
        var doctorReport = await LoadReportAsync(
            context,
            DevelopmentAnalysisReportSeeder.DoctorReportId
        );

        Assert.NotNull(doctorReport);
        var result = JsonSerializer.Deserialize<AnalysisReportResult>(
            doctorReport!.ReportJson,
            JsonOptions
        );

        Assert.NotNull(result);
        Assert.Equal(DevelopmentAnalysisReportSeeder.DoctorOneId, result!.DoctorId);

        var summary = Assert.Single(result.Doctors);
        Assert.Equal(DevelopmentAnalysisReportSeeder.DoctorOneId, summary.DoctorId);
        Assert.Equal(2, summary.TotalPatients);
        Assert.Equal(2, summary.TotalTreatmentPlans);
        Assert.Equal(1, summary.TotalAdherentPlans);
        Assert.Equal(1, summary.TotalNonAdherentPlans);
        Assert.Equal(0, summary.TotalPendingReviewPlans);
        Assert.Equal(2, summary.Medicines.Count);
    }

    [Fact]
    public async Task SeedAnalysisReports_SystemReportCarriesBothDoctorSummaries()
    {
        await SeedAsync();

        await using var context = new AnalysisDbContext(_options);
        var systemReport = await LoadReportAsync(
            context,
            DevelopmentAnalysisReportSeeder.SystemReportId
        );

        Assert.NotNull(systemReport);
        var result = JsonSerializer.Deserialize<AnalysisReportResult>(
            systemReport!.ReportJson,
            JsonOptions
        );

        Assert.NotNull(result);
        Assert.Equal(2, result!.Doctors.Count);
        Assert.Contains(
            result.Doctors,
            doctor => doctor.DoctorId == DevelopmentAnalysisReportSeeder.DoctorOneId
        );
        Assert.Contains(
            result.Doctors,
            doctor => doctor.DoctorId == DevelopmentAnalysisReportSeeder.DoctorTwoId
        );
    }

    [Fact]
    public async Task SeedAnalysisReports_SystemReportHasTheExpectedCounters()
    {
        await SeedAsync();

        await using var context = new AnalysisDbContext(_options);
        var systemReport = await LoadReportAsync(
            context,
            DevelopmentAnalysisReportSeeder.SystemReportId
        );

        Assert.NotNull(systemReport);
        var result = JsonSerializer.Deserialize<AnalysisReportResult>(
            systemReport!.ReportJson,
            JsonOptions
        );

        Assert.NotNull(result);
        Assert.Equal(4, result!.TotalPatients);
        Assert.Equal(5, result.TotalTreatmentPlans);
        Assert.Equal(2, result.TotalAdherentPlans);
        Assert.Equal(2, result.TotalNonAdherentPlans);
        Assert.Equal(1, result.TotalPendingReviewPlans);
        Assert.Equal(1, result.TotalDeceasedPatients);
        Assert.Equal(1, result.TotalRecoveredPatients);
        Assert.Equal(2, result.TotalOtherOutcomePatients);
    }

    [Fact]
    public async Task SeedAnalysisReports_WritesDoctorReportAuditEntryWithThePreferredId()
    {
        await SeedAsync();

        await using var context = new AnalysisDbContext(_options);
        var auditEntry = await context
            .AuditEntries.AsNoTracking()
            .SingleAsync(
                item => item.Id == DevelopmentAnalysisReportSeeder.DoctorReportCreateAuditId,
                TestContext.Current.CancellationToken
            );

        Assert.Equal(
            DevelopmentAnalysisReportSeeder.DoctorReportId,
            auditEntry.EntityId
        );
        Assert.Equal("Created", auditEntry.Operation);
    }

    [Fact]
    public async Task SeedAnalysisReports_WritesSystemReportAuditEntryWithThePreferredId()
    {
        await SeedAsync();

        await using var context = new AnalysisDbContext(_options);
        var auditEntry = await context
            .AuditEntries.AsNoTracking()
            .SingleAsync(
                item => item.Id == DevelopmentAnalysisReportSeeder.SystemReportCreateAuditId,
                TestContext.Current.CancellationToken
            );

        // The preferred audit entry id belongs to the system wide seeding scope.
        // That scope creates the system report and the report that is soft
        // deleted afterwards, so both rows are audited with the "Created" action
        // inside the same change set.
        Assert.Equal("Analysis.ClinicalAnalysis.Created", auditEntry.Action);
        Assert.Equal("Created", auditEntry.Operation);
        Assert.Equal(DevelopmentAnalysisReportSeeder.AdminActorId, auditEntry.ActorAccountId);
        Assert.Equal("Admin", auditEntry.ActorRole);
        Assert.Equal(
            Guid.Parse("93100000-0000-0000-0000-000000000002"),
            auditEntry.ChangeSetId
        );

        var systemReportAudits = await context
            .AuditEntries.AsNoTracking()
            .Where(item => item.EntityId == DevelopmentAnalysisReportSeeder.SystemReportId)
            .ToListAsync(TestContext.Current.CancellationToken);

        var systemReportAudit = Assert.Single(systemReportAudits);
        Assert.Equal("Analysis.ClinicalAnalysis.Created", systemReportAudit.Action);
        Assert.Equal("Created", systemReportAudit.Operation);
        Assert.Equal(auditEntry.ChangeSetId, systemReportAudit.ChangeSetId);
    }

    [Fact]
    public async Task SeedAnalysisReports_WritesSoftDeleteAuditEntryWithThePreferredId()
    {
        await SeedAsync();

        await using var context = new AnalysisDbContext(_options);
        var auditEntry = await context
            .AuditEntries.AsNoTracking()
            .SingleAsync(
                item => item.Id == DevelopmentAnalysisReportSeeder.ReportSoftDeleteAuditId,
                TestContext.Current.CancellationToken
            );

        Assert.Equal(
            DevelopmentAnalysisReportSeeder.SoftDeletedReportId,
            auditEntry.EntityId
        );
        Assert.Equal("SoftDeleted", auditEntry.Operation);
    }

    [Fact]
    public async Task SeedAnalysisReports_AuditEntriesCarryTheActorOfTheirScope()
    {
        await SeedAsync();

        await using var context = new AnalysisDbContext(_options);
        var doctorAudit = await context
            .AuditEntries.AsNoTracking()
            .SingleAsync(
                item => item.Id == DevelopmentAnalysisReportSeeder.DoctorReportCreateAuditId,
                TestContext.Current.CancellationToken
            );
        var systemAudit = await context
            .AuditEntries.AsNoTracking()
            .SingleAsync(
                item => item.Id == DevelopmentAnalysisReportSeeder.SystemReportCreateAuditId,
                TestContext.Current.CancellationToken
            );
        var softDeleteAudit = await context
            .AuditEntries.AsNoTracking()
            .SingleAsync(
                item => item.Id == DevelopmentAnalysisReportSeeder.ReportSoftDeleteAuditId,
                TestContext.Current.CancellationToken
            );

        Assert.Equal(DevelopmentAnalysisReportSeeder.DoctorOneId, doctorAudit.ActorAccountId);
        Assert.Equal("Doctor", doctorAudit.ActorRole);
        Assert.Equal(DevelopmentAnalysisReportSeeder.AdminActorId, systemAudit.ActorAccountId);
        Assert.Equal("Admin", systemAudit.ActorRole);
        Assert.Equal(
            DevelopmentAnalysisReportSeeder.AdminActorId,
            softDeleteAudit.ActorAccountId
        );
        Assert.Equal("Admin", softDeleteAudit.ActorRole);
    }

    [Fact]
    public async Task SeedAnalysisReports_IsIdempotent()
    {
        await SeedAsync();

        int reportCountAfterFirstSeed;
        int auditCountAfterFirstSeed;

        await using (var context = new AnalysisDbContext(_options))
        {
            reportCountAfterFirstSeed = await context
                .ClinicalAnalyses.IgnoreQueryFilters()
                .AsNoTracking()
                .CountAsync(TestContext.Current.CancellationToken);
            auditCountAfterFirstSeed = await context
                .AuditEntries.AsNoTracking()
                .CountAsync(TestContext.Current.CancellationToken);
        }

        await SeedAsync();

        await using var verifyContext = new AnalysisDbContext(_options);
        var reportCountAfterSecondSeed = await verifyContext
            .ClinicalAnalyses.IgnoreQueryFilters()
            .AsNoTracking()
            .CountAsync(TestContext.Current.CancellationToken);
        var auditCountAfterSecondSeed = await verifyContext
            .AuditEntries.AsNoTracking()
            .CountAsync(TestContext.Current.CancellationToken);

        Assert.Equal(3, reportCountAfterFirstSeed);
        Assert.Equal(reportCountAfterFirstSeed, reportCountAfterSecondSeed);
        Assert.Equal(auditCountAfterFirstSeed, auditCountAfterSecondSeed);

        // The seeded reports are still the same three rows
        var ids = await verifyContext
            .ClinicalAnalyses.IgnoreQueryFilters()
            .AsNoTracking()
            .Select(analysis => analysis.Id)
            .ToListAsync(TestContext.Current.CancellationToken);

        Assert.Contains(DevelopmentAnalysisReportSeeder.DoctorReportId, ids);
        Assert.Contains(DevelopmentAnalysisReportSeeder.SystemReportId, ids);
        Assert.Contains(DevelopmentAnalysisReportSeeder.SoftDeletedReportId, ids);
    }

    [Fact]
    public async Task SeedAnalysisReports_AuditEntriesCarryTheExpectedActionNames()
    {
        await SeedAsync();

        await using var context = new AnalysisDbContext(_options);
        var actions = await context
            .AuditEntries.AsNoTracking()
            .Select(item => item.Action)
            .ToListAsync(TestContext.Current.CancellationToken);

        Assert.Contains("Analysis.ClinicalAnalysis.Created", actions);
        Assert.Contains("Analysis.ClinicalAnalysis.SoftDeleted", actions);
    }

    [Fact]
    public async Task SeedAnalysisReports_SeededIdCollectionsHaveTheExpectedSize()
    {
        await SeedAsync();

        await using var context = new AnalysisDbContext(_options);
        var doctorReport = await LoadReportAsync(
            context,
            DevelopmentAnalysisReportSeeder.DoctorReportId
        );
        var systemReport = await LoadReportAsync(
            context,
            DevelopmentAnalysisReportSeeder.SystemReportId
        );
        var softDeletedReport = await LoadReportAsync(
            context,
            DevelopmentAnalysisReportSeeder.SoftDeletedReportId
        );

        Assert.NotNull(doctorReport);
        var doctorPatientIds = JsonSerializer.Deserialize<List<Guid>>(
            doctorReport!.PatientIdsJson
        );
        var doctorPlanIds = JsonSerializer.Deserialize<List<Guid>>(
            doctorReport.TreatmentPlanIdsJson
        );
        Assert.NotNull(doctorPatientIds);
        Assert.NotNull(doctorPlanIds);
        Assert.Equal(2, doctorPatientIds!.Count);
        Assert.Equal(2, doctorPlanIds!.Count);

        Assert.NotNull(systemReport);
        var systemPatientIds = JsonSerializer.Deserialize<List<Guid>>(
            systemReport!.PatientIdsJson
        );
        var systemPlanIds = JsonSerializer.Deserialize<List<Guid>>(
            systemReport.TreatmentPlanIdsJson
        );
        Assert.NotNull(systemPatientIds);
        Assert.NotNull(systemPlanIds);
        Assert.Equal(4, systemPatientIds!.Count);
        Assert.Equal(5, systemPlanIds!.Count);

        Assert.NotNull(softDeletedReport);
        var softDeletedPatientIds = JsonSerializer.Deserialize<List<Guid>>(
            softDeletedReport!.PatientIdsJson
        );
        var softDeletedPlanIds = JsonSerializer.Deserialize<List<Guid>>(
            softDeletedReport.TreatmentPlanIdsJson
        );
        Assert.NotNull(softDeletedPatientIds);
        Assert.NotNull(softDeletedPlanIds);
        Assert.Single(softDeletedPatientIds!);
        Assert.Single(softDeletedPlanIds!);
    }

    private async Task SeedAsync()
    {
        using var host = new HostBuilder()
            .ConfigureServices(services =>
            {
                services.AddLogging();
                services.AddAuditLogging();
                services.AddDbContext<AnalysisDbContext>(options =>
                    options.UseNpgsql(_fixture.ConnectionString)
                );
            })
            .Build();

        await host.SeedAnalysisReportsAsync();
    }

    private static Task<ClinicalAnalysis?> LoadReportAsync(
        AnalysisDbContext context,
        Guid reportId
    ) =>
        context
            .ClinicalAnalyses.IgnoreQueryFilters()
            .AsNoTracking()
            .SingleOrDefaultAsync(
                item => item.Id == reportId,
                TestContext.Current.CancellationToken
            );
}
