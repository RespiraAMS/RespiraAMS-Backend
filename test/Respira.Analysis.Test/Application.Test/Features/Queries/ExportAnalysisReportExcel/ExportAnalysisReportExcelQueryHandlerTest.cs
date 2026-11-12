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

    [Fact]
    public async Task Export_ExistingReport_FileNameContainsAnalysisIdAndXlsxExtension()
    {
        var analysisId = Guid.CreateVersion7();
        await SeedAsync(analysisId, Serialize(CreateReport(analysisId, null)));
        var handler = CreateHandler();

        var result = await handler.HandleAsync(
            new ExportAnalysisReportExcelQuery { AnalysisId = analysisId },
            TestContext.Current.CancellationToken
        );

        Assert.True(result.IsSuccess());
        Assert.NotNull(result.Data);
        Assert.EndsWith(".xlsx", result.Data.FileName);
        Assert.StartsWith($"analysis-report-{analysisId:N}-", result.Data.FileName);
        Assert.Contains(analysisId.ToString("N"), result.Data.FileName);
    }

    [Fact]
    public async Task Export_SameAnalysisTwice_ProducesDifferentFileNamesWithSameAnalysisId()
    {
        var analysisId = Guid.CreateVersion7();
        await SeedAsync(analysisId, Serialize(CreateReport(analysisId, null)));
        var handler = CreateHandler();

        var first = await handler.HandleAsync(
            new ExportAnalysisReportExcelQuery { AnalysisId = analysisId },
            TestContext.Current.CancellationToken
        );

        await WaitForNextSecondAsync();

        var second = await handler.HandleAsync(
            new ExportAnalysisReportExcelQuery { AnalysisId = analysisId },
            TestContext.Current.CancellationToken
        );

        Assert.True(first.IsSuccess());
        Assert.True(second.IsSuccess());
        Assert.NotNull(first.Data);
        Assert.NotNull(second.Data);

        Assert.NotEqual(first.Data.FileName, second.Data.FileName);
        Assert.Contains(analysisId.ToString("N"), first.Data.FileName);
        Assert.Contains(analysisId.ToString("N"), second.Data.FileName);
        Assert.Equal(analysisId, first.Data.AnalysisId);
        Assert.Equal(analysisId, second.Data.AnalysisId);
    }

    [Fact]
    public void Export_ContentTypeConstant_IsExcelSpreadsheetMimeType()
    {
        Assert.Equal(
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            ExportAnalysisReportExcelQueryHandler.ExcelContentType
        );
    }

    [Fact]
    public async Task Export_RequiredDoctorIdMatchingReport_Succeeds()
    {
        var analysisId = Guid.CreateVersion7();
        var doctorId = Guid.CreateVersion7();
        await SeedWithDoctorAsync(
            analysisId,
            Serialize(CreateReport(analysisId, doctorId)),
            doctorId
        );
        var handler = CreateHandler();

        var result = await handler.HandleAsync(
            new ExportAnalysisReportExcelQuery
            {
                AnalysisId = analysisId,
                RequiredDoctorId = doctorId,
            },
            TestContext.Current.CancellationToken
        );

        Assert.True(result.IsSuccess());
        Assert.Null(result.Error);
        Assert.Equal(ApplicationStatus.Success, result.StatusCode);
        Assert.NotNull(result.Data);
        Assert.Equal(analysisId, result.Data.AnalysisId);
        Assert.Equal(doctorId, result.Data.DoctorId);
    }

    [Fact]
    public async Task Export_RequiredDoctorIdDifferentFromReport_ReturnsRestricted()
    {
        var analysisId = Guid.CreateVersion7();
        var ownerDoctorId = Guid.CreateVersion7();
        var otherDoctorId = Guid.CreateVersion7();
        await SeedWithDoctorAsync(
            analysisId,
            Serialize(CreateReport(analysisId, ownerDoctorId)),
            ownerDoctorId
        );
        var handler = CreateHandler();

        var result = await handler.HandleAsync(
            new ExportAnalysisReportExcelQuery
            {
                AnalysisId = analysisId,
                RequiredDoctorId = otherDoctorId,
            },
            TestContext.Current.CancellationToken
        );

        Assert.True(result.IsFailure());
        Assert.Equal(ApplicationStatus.Restricted, result.StatusCode);
        Assert.NotNull(result.Error);
        Assert.Equal(ApplicationStatus.Restricted, result.Error.StatusCode);
        Assert.Null(result.Data);
    }

    [Fact]
    public async Task Export_StoredDoctorIdNullButReportOwnedByOther_ReturnsRestricted()
    {
        var analysisId = Guid.CreateVersion7();
        var ownerDoctorId = Guid.CreateVersion7();
        var otherDoctorId = Guid.CreateVersion7();
        await SeedWithDoctorAsync(
            analysisId,
            Serialize(CreateReport(analysisId, ownerDoctorId)),
            doctorId: null
        );
        var handler = CreateHandler();

        var result = await handler.HandleAsync(
            new ExportAnalysisReportExcelQuery
            {
                AnalysisId = analysisId,
                RequiredDoctorId = otherDoctorId,
            },
            TestContext.Current.CancellationToken
        );

        Assert.True(result.IsFailure());
        Assert.Equal(ApplicationStatus.Restricted, result.StatusCode);
        Assert.NotNull(result.Error);
        Assert.Null(result.Data);
    }

    [Fact]
    public async Task Export_RequiredDoctorIdNull_Succeeds()
    {
        var analysisId = Guid.CreateVersion7();
        var doctorId = Guid.CreateVersion7();
        await SeedWithDoctorAsync(
            analysisId,
            Serialize(CreateReport(analysisId, doctorId)),
            doctorId
        );
        var handler = CreateHandler();

        var result = await handler.HandleAsync(
            new ExportAnalysisReportExcelQuery
            {
                AnalysisId = analysisId,
                RequiredDoctorId = null,
            },
            TestContext.Current.CancellationToken
        );

        Assert.True(result.IsSuccess());
        Assert.Equal(ApplicationStatus.Success, result.StatusCode);
        Assert.NotNull(result.Data);
        Assert.Equal(analysisId, result.Data.AnalysisId);
    }

    [Fact]
    public async Task Export_PassesTheStoredReportToTheWriter()
    {
        var analysisId = Guid.CreateVersion7();
        var doctorId = Guid.CreateVersion7();
        var report = CreateReport(analysisId, doctorId) with
        {
            TotalPatients = 12,
            TotalTreatmentPlans = 34,
            TotalAdherentPlans = 20,
        };
        await SeedAsync(analysisId, Serialize(report));
        var writer = new CapturingExcelWriter();
        var handler = new ExportAnalysisReportExcelQueryHandler(_context, writer);

        var result = await handler.HandleAsync(
            new ExportAnalysisReportExcelQuery { AnalysisId = analysisId },
            TestContext.Current.CancellationToken
        );

        Assert.True(result.IsSuccess());
        Assert.Equal(1, writer.CallCount);
        Assert.NotNull(writer.Report);
        Assert.Equal(analysisId, writer.Report.AnalysisId);
        Assert.Equal(doctorId, writer.Report.DoctorId);
        Assert.Equal(12, writer.Report.TotalPatients);
        Assert.Equal(34, writer.Report.TotalTreatmentPlans);
        Assert.Equal(20, writer.Report.TotalAdherentPlans);
        Assert.Equal(_fileContent.Length, writer.Content.Length);
    }

    [Fact]
    public async Task Export_CarriesDoctorsAndMedicinesIntoTheWriter()
    {
        var analysisId = Guid.CreateVersion7();
        var doctorId = Guid.CreateVersion7();
        var medicineId = Guid.CreateVersion7();
        var report = CreateReportWithMedicines(analysisId, doctorId, medicineId);
        await SeedAsync(analysisId, Serialize(report));
        var writer = new CapturingExcelWriter();
        var handler = new ExportAnalysisReportExcelQueryHandler(_context, writer);

        var result = await handler.HandleAsync(
            new ExportAnalysisReportExcelQuery { AnalysisId = analysisId },
            TestContext.Current.CancellationToken
        );

        Assert.True(result.IsSuccess());
        Assert.NotNull(writer.Report);
        var doctor = Assert.Single(writer.Report.Doctors);
        Assert.Equal(doctorId, doctor.DoctorId);
        Assert.Equal(3, doctor.TotalPatients);
        var medicine = Assert.Single(doctor.Medicines);
        Assert.Equal(medicineId, medicine.MedicineId);
        Assert.Equal("Metformin", medicine.Name);
        Assert.Equal("Biguanide", medicine.Classification);
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

    private async Task SeedWithDoctorAsync(
        Guid analysisId,
        string reportJson,
        Guid? doctorId,
        bool softDeleted = false
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

    private static async Task WaitForNextSecondAsync()
    {
        var now = DateTimeOffset.UtcNow;
        var nextSecond = new DateTimeOffset(
            now.Year,
            now.Month,
            now.Day,
            now.Hour,
            now.Minute,
            now.Second,
            TimeSpan.Zero
        ).AddSeconds(1);

        var delay = nextSecond - now;
        if (delay > TimeSpan.Zero)
        {
            await Task.Delay(
                delay + TimeSpan.FromMilliseconds(50),
                TestContext.Current.CancellationToken
            );
        }
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

    private static AnalysisReportResult CreateReportWithMedicines(
        Guid analysisId,
        Guid doctorId,
        Guid medicineId
    ) =>
        new(
            analysisId,
            doctorId,
            Start,
            End,
            2,
            3,
            1,
            1,
            0,
            0,
            2,
            1,
            [
                new DoctorMedicineAnalysisSummary(
                    doctorId,
                    3,
                    2,
                    1,
                    1,
                    0,
                    [
                        new MedicineAnalysisSummary(
                            medicineId,
                            "Metformin",
                            "Biguanide",
                            2,
                            1,
                            1,
                            0
                        ),
                    ]
                ),
            ]
        );

    private sealed class StubExcelWriter(byte[] content) : IAnalysisReportExcelWriter
    {
        public byte[] Write(AnalysisReportResult report) => content;
    }

    private sealed class CapturingExcelWriter : IAnalysisReportExcelWriter
    {
        public AnalysisReportResult? Report { get; private set; }

        public int CallCount { get; private set; }

        public byte[] Content { get; private set; } = [];

        public byte[] Write(AnalysisReportResult report)
        {
            Report = report;
            CallCount++;
            Content = [1, 2, 3, 4];
            return Content;
        }
    }

    private sealed class ThrowingExcelWriter : IAnalysisReportExcelWriter
    {
        public byte[] Write(AnalysisReportResult report) =>
            throw new InvalidOperationException("boom");
    }
}
