using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Respira.Analysis.Application.Contracts.Export;
using Respira.Analysis.Application.Contracts;
using Respira.Analysis.Application.Features.Queries.ExportAnalysisReportExcel;
using Respira.Analysis.Domain.Entities;
using Respira.Analysis.Infrastructure.Data;
using Respira.ServiceDefaults.Contracts.Results;

namespace Respira.Analysis.Application.Test.Features.Queries.ExportAnalysisReportExcel;

public class ExportAnalysisReportExcelQueryHandlerTest
    : IClassFixture<PostgresFixture>,
        IAsyncLifetime
{
    private static readonly DateTimeOffset Start = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset End = new(2026, 2, 1, 0, 0, 0, TimeSpan.Zero);

    private readonly DbContextOptions<AnalysisDbContext> _options;
    private readonly AnalysisDbContext _context;
    private readonly byte[] _fileContent = [1, 2, 3, 4];

    public ExportAnalysisReportExcelQueryHandlerTest(PostgresFixture fixture)
    {
        _options = new DbContextOptionsBuilder<AnalysisDbContext>()
            .UseNpgsql(fixture.ConnectionString)
            .Options;
        _context = new AnalysisDbContext(_options);
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
    public async Task Export_ExistingReport_ReturnsExcelFile()
    {
        var analysisId = Guid.CreateVersion7();
        var report = CreateReport(analysisId, Guid.CreateVersion7());
        await SeedAsync(analysisId, Serialize(report));
        var handler = CreateHandler();

        var result = await handler.HandleAsync(
            new ExportAnalysisReportExcelQuery { AnalysisId = analysisId },
            TestContext.Current.CancellationToken
        );

        Assert.True(result.IsSuccess());
        Assert.Null(result.Error);
        Assert.Equal(ApplicationStatus.Success, result.StatusCode);
        Assert.NotNull(result.Data);
        Assert.Equal(analysisId, result.Data.AnalysisId);
        Assert.Equal(report.DoctorId, result.Data.DoctorId);
        Assert.Equal(_fileContent, result.Data.Content);
        Assert.EndsWith(".xlsx", result.Data.FileName);
        Assert.Contains(analysisId.ToString("N"), result.Data.FileName);
        Assert.Equal(ExportAnalysisReportExcelQueryHandler.ExcelContentType, result.Data.ContentType);
    }

    [Fact]
    public async Task Export_SystemWideReport_ReturnsNullDoctorId()
    {
        var analysisId = Guid.CreateVersion7();
        var report = CreateReport(analysisId, null);
        await SeedAsync(analysisId, Serialize(report));
        var handler = CreateHandler();

        var result = await handler.HandleAsync(
            new ExportAnalysisReportExcelQuery { AnalysisId = analysisId },
            TestContext.Current.CancellationToken
        );

        Assert.True(result.IsSuccess());
        Assert.NotNull(result.Data);
        Assert.Null(result.Data.DoctorId);
    }

    [Fact]
    public async Task Export_EmptyId_ReturnsBadRequest()
    {
        var handler = CreateHandler();

        var result = await handler.HandleAsync(
            new ExportAnalysisReportExcelQuery { AnalysisId = Guid.Empty },
            TestContext.Current.CancellationToken
        );

        Assert.True(result.IsFailure());
        Assert.Equal(ApplicationStatus.BadRequest, result.StatusCode);
    }

    [Fact]
    public async Task Export_UnknownId_ReturnsNotFound()
    {
        var handler = CreateHandler();

        var result = await handler.HandleAsync(
            new ExportAnalysisReportExcelQuery { AnalysisId = Guid.CreateVersion7() },
            TestContext.Current.CancellationToken
        );

        Assert.True(result.IsFailure());
        Assert.Equal(ApplicationStatus.ResourceNotFound, result.StatusCode);
    }

    [Fact]
    public async Task Export_SoftDeletedReport_ReturnsNotFound()
    {
        var analysisId = Guid.CreateVersion7();
        await SeedAsync(analysisId, Serialize(CreateReport(analysisId, null)), softDeleted: true);
        var handler = CreateHandler();

        var result = await handler.HandleAsync(
            new ExportAnalysisReportExcelQuery { AnalysisId = analysisId },
            TestContext.Current.CancellationToken
        );

        Assert.True(result.IsFailure());
        Assert.Equal(ApplicationStatus.ResourceNotFound, result.StatusCode);
    }

    [Fact]
    public async Task Export_MalformedReportJson_ReturnsServerError()
    {
        var analysisId = Guid.CreateVersion7();
        await SeedAsync(analysisId, "[]");
        var handler = CreateHandler();

        var result = await handler.HandleAsync(
            new ExportAnalysisReportExcelQuery { AnalysisId = analysisId },
            TestContext.Current.CancellationToken
        );

        Assert.True(result.IsFailure());
        Assert.Equal(ApplicationStatus.ServerError, result.StatusCode);
    }

    [Fact]
    public async Task Export_NullReportJson_ReturnsServerError()
    {
        var analysisId = Guid.CreateVersion7();
        await SeedAsync(analysisId, "null");
        var handler = CreateHandler();

        var result = await handler.HandleAsync(
            new ExportAnalysisReportExcelQuery { AnalysisId = analysisId },
            TestContext.Current.CancellationToken
        );

        Assert.True(result.IsFailure());
        Assert.Equal(ApplicationStatus.ServerError, result.StatusCode);
    }

    [Fact]
    public async Task Export_WriterFailure_ReturnsServerError()
    {
        var analysisId = Guid.CreateVersion7();
        await SeedAsync(analysisId, Serialize(CreateReport(analysisId, null)));
        var handler = new ExportAnalysisReportExcelQueryHandler(
            _context,
            new ThrowingExcelWriter()
        );

        var result = await handler.HandleAsync(
            new ExportAnalysisReportExcelQuery { AnalysisId = analysisId },
            TestContext.Current.CancellationToken
        );

        Assert.True(result.IsFailure());
        Assert.Equal(ApplicationStatus.ServerError, result.StatusCode);
    }

    private ExportAnalysisReportExcelQueryHandler CreateHandler() =>
        new(_context, new StubExcelWriter(_fileContent));

    private async Task SeedAsync(Guid analysisId, string reportJson, bool softDeleted = false)
    {
        await using var context = new AnalysisDbContext(_options);
        context.ClinicalAnalyses.Add(
            new ClinicalAnalysis
            {
                Id = analysisId,
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

    private static AnalysisReportResult CreateReport(Guid analysisId, Guid? doctorId) =>
        new(
            analysisId,
            doctorId,
            Start,
            End,
            1,
            1,
            1,
            0,
            0,
            0,
            1,
            0,
            [
                new DoctorMedicineAnalysisSummary(
                    doctorId ?? Guid.CreateVersion7(),
                    1,
                    1,
                    1,
                    0,
                    0,
                    []
                ),
            ]
        );

    private sealed class StubExcelWriter(byte[] content) : IAnalysisReportExcelWriter
    {
        public byte[] Write(AnalysisReportResult report) => content;
    }

    private sealed class ThrowingExcelWriter : IAnalysisReportExcelWriter
    {
        public byte[] Write(AnalysisReportResult report) =>
            throw new InvalidOperationException("boom");
    }
}
