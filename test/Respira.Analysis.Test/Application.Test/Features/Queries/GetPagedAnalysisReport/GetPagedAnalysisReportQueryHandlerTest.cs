using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Respira.Analysis.Application.Contracts;
using Respira.Analysis.Application.Features.Queries.GetPagedAnalysisReport;
using Respira.Analysis.Domain.Entities;
using Respira.Analysis.Infrastructure.Data;
using Respira.ServiceDefaults.Contracts.Pagination;
using Respira.ServiceDefaults.Contracts.Results;

namespace Respira.Analysis.Application.Test.Features.Queries.GetPagedAnalysisReport;

public class GetPagedAnalysisReportQueryHandlerTest
    : IClassFixture<PostgresFixture>,
        IAsyncLifetime
{
    private static readonly DateTimeOffset Start = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset End = new(2026, 2, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset BaseCreatedAt = new(2026, 3, 1, 0, 0, 0, TimeSpan.Zero);

    private readonly DbContextOptions<AnalysisDbContext> _options;
    private readonly AnalysisDbContext _context;
    private readonly GetPagedAnalysisReportQueryHandler _handler;

    public GetPagedAnalysisReportQueryHandlerTest(PostgresFixture fixture)
    {
        _options = new DbContextOptionsBuilder<AnalysisDbContext>()
            .UseNpgsql(fixture.ConnectionString)
            .Options;

        _context = new AnalysisDbContext(_options);
        _handler = new GetPagedAnalysisReportQueryHandler(_context);
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
    public async Task GetPaged_ReturnsNewestFirstWithSummary()
    {
        var doctorId = Guid.CreateVersion7();
        var older = await SeedAsync(doctorId, BaseCreatedAt, totalPatients: 3, totalPlans: 4);
        var newer = await SeedAsync(
            doctorId,
            BaseCreatedAt.AddDays(1),
            totalPatients: 7,
            totalPlans: 9
        );

        var result = await HandleAsync(new PaginationParam { Page = 1, Size = 10 });

        Assert.True(result.IsSuccess());
        Assert.NotNull(result.Data);
        Assert.Equal(2, result.Data.Metadata.TotalItemCount);
        Assert.Equal(1, result.Data.Metadata.PageCount);
        Assert.True(result.Data.Metadata.HasPreviousPage is false);
        Assert.True(result.Data.Metadata.HasNextPage is false);

        var items = result.Data.Items.ToList();
        Assert.Equal(newer.Id, items[0].AnalysisId);
        Assert.Equal(7, items[0].TotalPatients);
        Assert.Equal(9, items[0].TotalTreatmentPlans);
        Assert.Equal(older.Id, items[1].AnalysisId);
        Assert.Equal(3, items[1].TotalPatients);
    }

    [Fact]
    public async Task GetPaged_FiltersByDoctor()
    {
        var doctorA = Guid.CreateVersion7();
        var doctorB = Guid.CreateVersion7();
        await SeedAsync(doctorA, BaseCreatedAt, totalPatients: 1, totalPlans: 1);
        await SeedAsync(doctorB, BaseCreatedAt, totalPatients: 2, totalPlans: 2);

        var result = await HandleAsync(
            new PaginationParam { Page = 1, Size = 10 },
            new AnalysisReportFilter { DoctorId = doctorA }
        );

        Assert.True(result.IsSuccess());
        Assert.NotNull(result.Data);
        var item = Assert.Single(result.Data.Items);
        Assert.Equal(doctorA, item.DoctorId);
    }

    [Fact]
    public async Task GetPaged_FiltersByCreatedAtRange()
    {
        var doctorId = Guid.CreateVersion7();
        await SeedAsync(doctorId, BaseCreatedAt, totalPatients: 1, totalPlans: 1);
        var inRange = await SeedAsync(
            doctorId,
            BaseCreatedAt.AddDays(5),
            totalPatients: 2,
            totalPlans: 2
        );
        await SeedAsync(doctorId, BaseCreatedAt.AddDays(10), totalPatients: 3, totalPlans: 3);

        var result = await HandleAsync(
            new PaginationParam { Page = 1, Size = 10 },
            new AnalysisReportFilter
            {
                CreatedFrom = BaseCreatedAt.AddDays(2),
                CreatedTo = BaseCreatedAt.AddDays(7),
            }
        );

        Assert.True(result.IsSuccess());
        Assert.NotNull(result.Data);
        var item = Assert.Single(result.Data.Items);
        Assert.Equal(inRange.Id, item.AnalysisId);
    }

    [Fact]
    public async Task GetPaged_ExcludesSoftDeletedReports()
    {
        var doctorId = Guid.CreateVersion7();
        var live = await SeedAsync(doctorId, BaseCreatedAt, totalPatients: 1, totalPlans: 1);
        await SeedAsync(
            doctorId,
            BaseCreatedAt.AddDays(1),
            totalPatients: 2,
            totalPlans: 2,
            softDeleted: true
        );

        var result = await HandleAsync(new PaginationParam { Page = 1, Size = 10 });

        Assert.True(result.IsSuccess());
        Assert.NotNull(result.Data);
        var item = Assert.Single(result.Data.Items);
        Assert.Equal(live.Id, item.AnalysisId);
    }

    [Fact]
    public async Task GetPaged_SecondPage_ReturnsRemainingItems()
    {
        var doctorId = Guid.CreateVersion7();
        for (var index = 0; index < 3; index++)
        {
            await SeedAsync(
                doctorId,
                BaseCreatedAt.AddDays(index),
                totalPatients: index + 1,
                totalPlans: index + 1
            );
        }

        var result = await HandleAsync(new PaginationParam { Page = 2, Size = 2 });

        Assert.True(result.IsSuccess());
        Assert.NotNull(result.Data);
        Assert.Equal(3, result.Data.Metadata.TotalItemCount);
        Assert.Equal(2, result.Data.Metadata.PageCount);
        Assert.Equal(2, result.Data.Metadata.CurrentPage);
        Assert.True(result.Data.Metadata.HasPreviousPage);
        Assert.True(result.Data.Metadata.HasNextPage is false);
        var item = Assert.Single(result.Data.Items);
        Assert.Equal(1, item.TotalPatients);
    }

    [Fact]
    public async Task GetPaged_InvalidPagination_ReturnsBadRequest()
    {
        var result = await HandleAsync(new PaginationParam { Page = 0, Size = 10 });

        Assert.True(result.IsFailure());
        Assert.Equal(ApplicationStatus.BadRequest, result.StatusCode);
    }

    [Fact]
    public async Task GetPaged_PageSizeAboveLimit_ReturnsBadRequest()
    {
        var result = await HandleAsync(new PaginationParam { Page = 1, Size = 101 });

        Assert.True(result.IsFailure());
        Assert.Equal(ApplicationStatus.BadRequest, result.StatusCode);
    }

    [Fact]
    public async Task GetPaged_InvalidCreatedRange_ReturnsBadRequest()
    {
        var result = await HandleAsync(
            new PaginationParam { Page = 1, Size = 10 },
            new AnalysisReportFilter
            {
                CreatedFrom = BaseCreatedAt.AddDays(2),
                CreatedTo = BaseCreatedAt,
            }
        );

        Assert.True(result.IsFailure());
        Assert.Equal(ApplicationStatus.BadRequest, result.StatusCode);
    }

    [Fact]
    public async Task GetPaged_CorruptedReportJson_ReturnsRowWithZeroTotals()
    {
        var doctorId = Guid.CreateVersion7();
        var seeded = await SeedAsync(doctorId, BaseCreatedAt, reportJson: "[]");

        var result = await HandleAsync(new PaginationParam { Page = 1, Size = 10 });

        Assert.True(result.IsSuccess());
        Assert.NotNull(result.Data);
        var item = Assert.Single(result.Data.Items);
        Assert.Equal(seeded.Id, item.AnalysisId);
        Assert.Equal(0, item.TotalPatients);
        Assert.Equal(0, item.TotalTreatmentPlans);
    }

    private async Task<Result<Pagination<AnalysisReportListItem>>> HandleAsync(
        PaginationParam param,
        AnalysisReportFilter? filter = null
    ) =>
        await _handler.HandleAsync(
            new GetPagedAnalysisReportQuery { Param = param, Filter = filter },
            TestContext.Current.CancellationToken
        );

    private async Task<ClinicalAnalysis> SeedAsync(
        Guid? doctorId,
        DateTimeOffset createdAt,
        int totalPatients = 0,
        int totalPlans = 0,
        string? reportJson = null,
        bool softDeleted = false
    )
    {
        await using var context = new AnalysisDbContext(_options);
        var analysis = new ClinicalAnalysis
        {
            DoctorId = doctorId,
            Start = Start,
            End = End,
            ReportJson = reportJson ?? CreateReportJson(doctorId, totalPatients, totalPlans),
            CreatedAt = createdAt,
            UpdatedAt = createdAt,
            IsDeleted = softDeleted,
            DeletedAt = softDeleted ? createdAt : null,
        };

        context.ClinicalAnalyses.Add(analysis);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        return analysis;
    }

    private static string CreateReportJson(Guid? doctorId, int totalPatients, int totalPlans)
    {
        var report = new AnalysisReportResult(
            Guid.CreateVersion7(),
            doctorId,
            Start,
            End,
            totalPlans,
            totalPatients,
            totalPlans,
            0,
            0,
            0,
            totalPatients,
            0,
            []
        );

        return JsonSerializer.Serialize(report, new JsonSerializerOptions(JsonSerializerDefaults.Web));
    }
}
