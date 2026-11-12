using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Respira.Analysis.Application.Contracts;
using Respira.Analysis.Application.Features.Queries.GetAnalysisReport;
using Respira.Analysis.Domain.Entities;
using Respira.Analysis.Infrastructure.Data;
using Respira.ServiceDefaults.Contracts.Results;

namespace Respira.Analysis.Application.Test.Features.Queries.GetAnalysisReport;

public class GetAnalysisReportQueryHandlerTest
    : IClassFixture<PostgresFixture>,
        IAsyncLifetime
{
    private static readonly DateTimeOffset Start = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset End = new(2026, 2, 1, 0, 0, 0, TimeSpan.Zero);

    private readonly DbContextOptions<AnalysisDbContext> _options;
    private readonly AnalysisDbContext _context;
    private readonly GetAnalysisReportQueryHandler _handler;

    public GetAnalysisReportQueryHandlerTest(PostgresFixture fixture)
    {
        _options = new DbContextOptionsBuilder<AnalysisDbContext>()
            .UseNpgsql(fixture.ConnectionString)
            .Options;

        _context = new AnalysisDbContext(_options);
        _handler = new GetAnalysisReportQueryHandler(_context);
    }

    public async ValueTask InitializeAsync()
    {
        await using var context = new AnalysisDbContext(_options);
        await context
            .ClinicalAnalyses.IgnoreQueryFilters()
            .ExecuteDeleteAsync(TestContext.Current.CancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        await _context.DisposeAsync();
    }

    [Fact]
    public async Task Get_ExistingReport_ReturnsStoredCounts()
    {
        var doctorId = Guid.CreateVersion7();
        var report = CreateReport(
            doctorId,
            totalPatients: 5,
            totalPlans: 7,
            totalAdherentPlans: 3,
            totalNonAdherentPlans: 2,
            totalPendingReviewPlans: 1,
            totalDeceasedPatients: 1,
            totalRecoveredPatients: 2,
            totalOtherOutcomePatients: 2
        );
        var analysisId = await SeedAsync(report, doctorId);

        var result = await HandleAsync(new GetAnalysisReportQuery { AnalysisId = analysisId });

        Assert.True(result.IsSuccess());
        Assert.Equal(ApplicationStatus.Success, result.StatusCode);
        Assert.NotNull(result.Data);
        Assert.Equal(analysisId, result.Data.AnalysisId);
        Assert.Equal(doctorId, result.Data.DoctorId);
        Assert.Equal(5, result.Data.TotalPatients);
        Assert.Equal(7, result.Data.TotalTreatmentPlans);
        Assert.Equal(3, result.Data.TotalAdherentPlans);
        Assert.Equal(2, result.Data.TotalNonAdherentPlans);
        Assert.Equal(1, result.Data.TotalPendingReviewPlans);
        Assert.Equal(1, result.Data.TotalDeceasedPatients);
        Assert.Equal(2, result.Data.TotalRecoveredPatients);
        Assert.Equal(2, result.Data.TotalOtherOutcomePatients);
    }

    [Fact]
    public async Task Get_ExistingReport_ReturnsDoctorsAndMedicines()
    {
        var doctorId = Guid.CreateVersion7();
        var report = CreateReport(
            doctorId,
            totalPatients: 2,
            totalPlans: 2,
            totalAdherentPlans: 1,
            totalNonAdherentPlans: 1,
            totalPendingReviewPlans: 0
        );
        var analysisId = await SeedAsync(report, doctorId);

        var result = await HandleAsync(new GetAnalysisReportQuery { AnalysisId = analysisId });

        Assert.True(result.IsSuccess());
        Assert.NotNull(result.Data);
        var doctor = Assert.Single(result.Data.Doctors);
        Assert.Equal(doctorId, doctor.DoctorId);
        Assert.Equal(2, doctor.TotalPatients);
        Assert.Equal(2, doctor.TotalTreatmentPlans);
        Assert.Equal(1, doctor.TotalAdherentPlans);
        Assert.Equal(1, doctor.TotalNonAdherentPlans);
        Assert.Equal(0, doctor.TotalPendingReviewPlans);
        var medicine = Assert.Single(doctor.Medicines);
        Assert.Equal("Amoxicillin", medicine.Name);
        Assert.Equal("Penicillin", medicine.Classification);
        Assert.Equal(2, medicine.TotalTreatmentPlans);
        Assert.Equal(1, medicine.TotalAdherentPlans);
        Assert.Equal(1, medicine.TotalNonAdherentPlans);
        Assert.Equal(0, medicine.TotalPendingReviewPlans);
    }

    [Fact]
    public async Task Get_EmptyAnalysisId_ReturnsBadRequest()
    {
        var result = await HandleAsync(new GetAnalysisReportQuery { AnalysisId = Guid.Empty });

        Assert.True(result.IsFailure());
        Assert.Equal(ApplicationStatus.BadRequest, result.StatusCode);
        Assert.Equal("Analysis ID is required", result.Error?.Description);
    }

    [Fact]
    public async Task Get_UnknownId_ReturnsNotFound()
    {
        var result = await HandleAsync(
            new GetAnalysisReportQuery { AnalysisId = Guid.CreateVersion7() }
        );

        Assert.True(result.IsFailure());
        Assert.Equal(ApplicationStatus.ResourceNotFound, result.StatusCode);
        Assert.Equal("Analysis report was not found", result.Error?.Description);
    }

    [Fact]
    public async Task Get_SoftDeletedReport_ReturnsNotFound()
    {
        var doctorId = Guid.CreateVersion7();
        var report = CreateReport(doctorId);
        var analysisId = await SeedAsync(report, doctorId, softDeleted: true);

        var result = await HandleAsync(new GetAnalysisReportQuery { AnalysisId = analysisId });

        Assert.True(result.IsFailure());
        Assert.Equal(ApplicationStatus.ResourceNotFound, result.StatusCode);
        Assert.Equal("Analysis report was not found", result.Error?.Description);
    }

    [Fact]
    public async Task Get_CorruptedReportJson_ReturnsServerError()
    {
        var analysisId = await SeedAsync("[]", null);

        var result = await HandleAsync(new GetAnalysisReportQuery { AnalysisId = analysisId });

        Assert.True(result.IsFailure());
        Assert.Equal(ApplicationStatus.ServerError, result.StatusCode);
        Assert.Equal("Stored analysis report is invalid", result.Error?.Description);
    }

    [Fact]
    public async Task Get_NullReportJson_ReturnsServerError()
    {
        var analysisId = await SeedAsync("null", null);

        var result = await HandleAsync(new GetAnalysisReportQuery { AnalysisId = analysisId });

        Assert.True(result.IsFailure());
        Assert.Equal(ApplicationStatus.ServerError, result.StatusCode);
        Assert.Equal("Stored analysis report is invalid", result.Error?.Description);
    }

    [Fact]
    public async Task Get_RequiredDoctorIdMatchesRow_ReturnsSuccess()
    {
        var doctorId = Guid.CreateVersion7();
        var report = CreateReport(doctorId);
        var analysisId = await SeedAsync(report, doctorId);

        var result = await HandleAsync(
            new GetAnalysisReportQuery
            {
                AnalysisId = analysisId,
                RequiredDoctorId = doctorId,
            }
        );

        Assert.True(result.IsSuccess());
        Assert.Equal(ApplicationStatus.Success, result.StatusCode);
        Assert.NotNull(result.Data);
        Assert.Equal(doctorId, result.Data.DoctorId);
    }

    [Fact]
    public async Task Get_RequiredDoctorIdDiffersFromRow_ReturnsRestricted()
    {
        var doctorId = Guid.CreateVersion7();
        var report = CreateReport(doctorId);
        var analysisId = await SeedAsync(report, doctorId);

        var result = await HandleAsync(
            new GetAnalysisReportQuery
            {
                AnalysisId = analysisId,
                RequiredDoctorId = Guid.CreateVersion7(),
            }
        );

        Assert.True(result.IsFailure());
        Assert.Equal(ApplicationStatus.Restricted, result.StatusCode);
        Assert.Equal("You can only access your own analysis reports", result.Error?.Description);
    }

    [Fact]
    public async Task Get_RowDoctorIdIsNullAndReportDoctorIdMatches_ReturnsSuccess()
    {
        var doctorId = Guid.CreateVersion7();
        var report = CreateReport(doctorId);
        var analysisId = await SeedAsync(report, null);

        var result = await HandleAsync(
            new GetAnalysisReportQuery
            {
                AnalysisId = analysisId,
                RequiredDoctorId = doctorId,
            }
        );

        Assert.True(result.IsSuccess());
        Assert.Equal(ApplicationStatus.Success, result.StatusCode);
        Assert.NotNull(result.Data);
        Assert.Equal(doctorId, result.Data.DoctorId);
    }

    [Fact]
    public async Task Get_RowDoctorIdIsNullButReportDoctorIdDiffers_ReturnsRestricted()
    {
        var report = CreateReport(Guid.CreateVersion7());
        var analysisId = await SeedAsync(report, null);

        var result = await HandleAsync(
            new GetAnalysisReportQuery
            {
                AnalysisId = analysisId,
                RequiredDoctorId = Guid.CreateVersion7(),
            }
        );

        Assert.True(result.IsFailure());
        Assert.Equal(ApplicationStatus.Restricted, result.StatusCode);
        Assert.Equal("You can only access your own analysis reports", result.Error?.Description);
    }

    [Fact]
    public async Task Get_RowAndReportDoctorIdAreNull_ReturnsRestricted()
    {
        var report = CreateReport(null);
        var analysisId = await SeedAsync(report, null);

        var result = await HandleAsync(
            new GetAnalysisReportQuery
            {
                AnalysisId = analysisId,
                RequiredDoctorId = Guid.CreateVersion7(),
            }
        );

        Assert.True(result.IsFailure());
        Assert.Equal(ApplicationStatus.Restricted, result.StatusCode);
        Assert.Equal("You can only access your own analysis reports", result.Error?.Description);
    }

    [Fact]
    public async Task Get_RequiredDoctorIdNull_ReturnsSuccessRegardlessOfOwner()
    {
        var doctorId = Guid.CreateVersion7();
        var report = CreateReport(doctorId);
        var analysisId = await SeedAsync(report, doctorId);

        var result = await HandleAsync(
            new GetAnalysisReportQuery
            {
                AnalysisId = analysisId,
                RequiredDoctorId = null,
            }
        );

        Assert.True(result.IsSuccess());
        Assert.Equal(ApplicationStatus.Success, result.StatusCode);
        Assert.NotNull(result.Data);
        Assert.Equal(doctorId, result.Data.DoctorId);
    }

    [Fact]
    public async Task Get_Success_HasNoError()
    {
        var doctorId = Guid.CreateVersion7();
        var report = CreateReport(doctorId);
        var analysisId = await SeedAsync(report, doctorId);

        var result = await HandleAsync(new GetAnalysisReportQuery { AnalysisId = analysisId });

        Assert.True(result.IsSuccess());
        Assert.Null(result.Error);
    }

    private Task<Result<AnalysisReportResult>> HandleAsync(GetAnalysisReportQuery query) =>
        _handler.HandleAsync(query, TestContext.Current.CancellationToken);

    private async Task<Guid> SeedAsync(
        AnalysisReportResult report,
        Guid? doctorId,
        bool softDeleted = false
    )
    {
        await SeedRawAsync(report.AnalysisId, doctorId, Serialize(report), softDeleted);
        return report.AnalysisId;
    }

    private async Task<Guid> SeedAsync(string reportJson, Guid? doctorId, bool softDeleted = false)
    {
        var analysisId = Guid.CreateVersion7();
        await SeedRawAsync(analysisId, doctorId, reportJson, softDeleted);
        return analysisId;
    }

    private async Task SeedRawAsync(
        Guid analysisId,
        Guid? doctorId,
        string reportJson,
        bool softDeleted
    )
    {
        await using var context = new AnalysisDbContext(_options);
        context.ClinicalAnalyses.Add(
            new ClinicalAnalysis
            {
                Id = analysisId,
                DoctorId = doctorId,
                Start = Start,
                End = End,
                ReportJson = reportJson,
                IsDeleted = softDeleted,
                DeletedAt = softDeleted ? End : null,
            }
        );
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    private static string Serialize(AnalysisReportResult report) =>
        JsonSerializer.Serialize(report, new JsonSerializerOptions(JsonSerializerDefaults.Web));

    private static AnalysisReportResult CreateReport(
        Guid? doctorId,
        int totalPatients = 1,
        int totalPlans = 1,
        int totalAdherentPlans = 1,
        int totalNonAdherentPlans = 0,
        int totalPendingReviewPlans = 0,
        int totalDeceasedPatients = 0,
        int totalRecoveredPatients = 0,
        int totalOtherOutcomePatients = 0
    ) =>
        new(
            Guid.CreateVersion7(),
            doctorId,
            Start,
            End,
            totalPlans,
            totalPatients,
            totalAdherentPlans,
            totalNonAdherentPlans,
            totalPendingReviewPlans,
            totalDeceasedPatients,
            totalRecoveredPatients,
            totalOtherOutcomePatients,
            [
                new DoctorMedicineAnalysisSummary(
                    doctorId ?? Guid.CreateVersion7(),
                    totalPatients,
                    totalPlans,
                    totalAdherentPlans,
                    totalNonAdherentPlans,
                    totalPendingReviewPlans,
                    [
                        new MedicineAnalysisSummary(
                            Guid.CreateVersion7(),
                            "Amoxicillin",
                            "Penicillin",
                            totalPlans,
                            totalAdherentPlans,
                            totalNonAdherentPlans,
                            totalPendingReviewPlans
                        ),
                    ]
                ),
            ]
        );
}
