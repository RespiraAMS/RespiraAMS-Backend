using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Respira.Analysis.Application.Contracts;
using Respira.Analysis.Application.Features.Commands.DeleteAnalysisReport;
using Respira.Analysis.Application.Features.Queries.GetAnalysisReport;
using Respira.Analysis.Domain.Entities;
using Respira.Analysis.Infrastructure.Data;
using Respira.ServiceDefaults.Contracts.Results;

namespace Respira.Analysis.Application.Test.Features.Commands.DeleteAnalysisReport;

public class DeleteAnalysisReportCommandHandlerTest
    : IClassFixture<PostgresFixture>,
        IAsyncLifetime
{
    private static readonly DateTimeOffset Start = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset End = new(2026, 2, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset OldTimestamp = new(2020, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private readonly DbContextOptions<AnalysisDbContext> _options;
    private readonly AnalysisDbContext _context;
    private readonly DeleteAnalysisReportCommandHandler _handler;

    public DeleteAnalysisReportCommandHandlerTest(PostgresFixture fixture)
    {
        _options = new DbContextOptionsBuilder<AnalysisDbContext>()
            .UseNpgsql(fixture.ConnectionString)
            .Options;

        _context = new AnalysisDbContext(_options);
        _handler = new DeleteAnalysisReportCommandHandler(
            _context,
            NullLogger<DeleteAnalysisReportCommandHandler>.Instance
        );
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
    public async Task Delete_EmptyAnalysisId_ReturnsBadRequest()
    {
        var result = await HandleAsync(
            new DeleteAnalysisReportCommand
            {
                AnalysisId = Guid.Empty,
                ActorAccountId = Guid.CreateVersion7(),
                ActorRole = "Admin",
            }
        );

        Assert.True(result.IsFailure());
        Assert.Equal(ApplicationStatus.BadRequest, result.StatusCode);
        Assert.Equal("Analysis ID is required", result.Error?.Description);
    }

    [Fact]
    public async Task Delete_UnknownId_ReturnsNotFound()
    {
        var result = await HandleAsync(
            new DeleteAnalysisReportCommand
            {
                AnalysisId = Guid.CreateVersion7(),
                ActorAccountId = Guid.CreateVersion7(),
                ActorRole = "Admin",
            }
        );

        Assert.True(result.IsFailure());
        Assert.Equal(ApplicationStatus.ResourceNotFound, result.StatusCode);
        Assert.Equal("Analysis report was not found", result.Error?.Description);
    }

    [Fact]
    public async Task Delete_AlreadySoftDeleted_ReturnsNotFound()
    {
        var analysisId = await SeedAsync(Guid.CreateVersion7(), softDeleted: true);

        var result = await HandleAsync(
            new DeleteAnalysisReportCommand
            {
                AnalysisId = analysisId,
                ActorAccountId = Guid.CreateVersion7(),
                ActorRole = "Admin",
            }
        );

        Assert.True(result.IsFailure());
        Assert.Equal(ApplicationStatus.ResourceNotFound, result.StatusCode);
        Assert.Equal("Analysis report was not found", result.Error?.Description);
    }

    [Fact]
    public async Task Delete_Admin_DeletesAnyReport()
    {
        var ownerId = Guid.CreateVersion7();
        var analysisId = await SeedAsync(ownerId);

        var result = await HandleAsync(
            new DeleteAnalysisReportCommand
            {
                AnalysisId = analysisId,
                ActorAccountId = Guid.CreateVersion7(),
                ActorRole = "Admin",
            }
        );

        Assert.True(result.IsSuccess());
        Assert.Equal(ApplicationStatus.Success, result.StatusCode);

        var stored = await LoadAnalysisAsync(analysisId);
        Assert.NotNull(stored);
        Assert.True(stored.IsDeleted);
        Assert.NotNull(stored.DeletedAt);
        Assert.True(stored.UpdatedAt > OldTimestamp);
    }

    [Fact]
    public async Task Delete_HeadDepartmentDoctor_DeletesAnyReport()
    {
        var ownerId = Guid.CreateVersion7();
        var analysisId = await SeedAsync(ownerId);

        var result = await HandleAsync(
            new DeleteAnalysisReportCommand
            {
                AnalysisId = analysisId,
                ActorAccountId = Guid.CreateVersion7(),
                ActorRole = "HeadDepartmentDoctor",
            }
        );

        Assert.True(result.IsSuccess());
        Assert.Equal(ApplicationStatus.Success, result.StatusCode);

        var stored = await LoadAnalysisAsync(analysisId);
        Assert.NotNull(stored);
        Assert.True(stored.IsDeleted);
    }

    [Theory]
    [InlineData("admin")]
    [InlineData("ADMIN")]
    [InlineData("headdepartmentdoctor")]
    [InlineData("HeadDepartmentDoctor")]
    public async Task Delete_RoleComparisonIsCaseInsensitive(string role)
    {
        var ownerId = Guid.CreateVersion7();
        var analysisId = await SeedAsync(ownerId);

        var result = await HandleAsync(
            new DeleteAnalysisReportCommand
            {
                AnalysisId = analysisId,
                ActorAccountId = Guid.CreateVersion7(),
                ActorRole = role,
            }
        );

        Assert.True(result.IsSuccess());
        Assert.Equal(ApplicationStatus.Success, result.StatusCode);

        var stored = await LoadAnalysisAsync(analysisId);
        Assert.NotNull(stored);
        Assert.True(stored.IsDeleted);
    }

    [Fact]
    public async Task Delete_OwnerDoctor_DeletesOwnReport()
    {
        var ownerId = Guid.CreateVersion7();
        var analysisId = await SeedAsync(ownerId);

        var result = await HandleAsync(
            new DeleteAnalysisReportCommand
            {
                AnalysisId = analysisId,
                ActorAccountId = ownerId,
                ActorRole = "Doctor",
            }
        );

        Assert.True(result.IsSuccess());
        Assert.Equal(ApplicationStatus.Success, result.StatusCode);

        var stored = await LoadAnalysisAsync(analysisId);
        Assert.NotNull(stored);
        Assert.True(stored.IsDeleted);
    }

    [Fact]
    public async Task Delete_NullActorRole_OwnerStillAllowed()
    {
        var ownerId = Guid.CreateVersion7();
        var analysisId = await SeedAsync(ownerId);

        var result = await HandleAsync(
            new DeleteAnalysisReportCommand
            {
                AnalysisId = analysisId,
                ActorAccountId = ownerId,
                ActorRole = null!,
            }
        );

        Assert.True(result.IsSuccess());
        Assert.Equal(ApplicationStatus.Success, result.StatusCode);
    }

    [Fact]
    public async Task Delete_NullActorRole_NonOwnerIsRestricted()
    {
        var analysisId = await SeedAsync(Guid.CreateVersion7());

        var result = await HandleAsync(
            new DeleteAnalysisReportCommand
            {
                AnalysisId = analysisId,
                ActorAccountId = Guid.CreateVersion7(),
                ActorRole = null!,
            }
        );

        Assert.True(result.IsFailure());
        Assert.Equal(ApplicationStatus.Restricted, result.StatusCode);
        Assert.Equal("You can only delete your own analysis reports", result.Error?.Description);
    }

    [Fact]
    public async Task Delete_ActorRoleWithWhitespace_IsTrimmed()
    {
        var analysisId = await SeedAsync(Guid.CreateVersion7());

        var result = await HandleAsync(
            new DeleteAnalysisReportCommand
            {
                AnalysisId = analysisId,
                ActorAccountId = Guid.CreateVersion7(),
                ActorRole = "  Admin  ",
            }
        );

        Assert.True(result.IsSuccess());
        Assert.Equal(ApplicationStatus.Success, result.StatusCode);

        var stored = await LoadAnalysisAsync(analysisId);
        Assert.NotNull(stored);
        Assert.True(stored.IsDeleted);
    }

    [Fact]
    public async Task Delete_DifferentDoctor_IsRestricted()
    {
        var ownerId = Guid.CreateVersion7();
        var analysisId = await SeedAsync(ownerId);

        var result = await HandleAsync(
            new DeleteAnalysisReportCommand
            {
                AnalysisId = analysisId,
                ActorAccountId = Guid.CreateVersion7(),
                ActorRole = "Doctor",
            }
        );

        Assert.True(result.IsFailure());
        Assert.Equal(ApplicationStatus.Restricted, result.StatusCode);
        Assert.Equal("You can only delete your own analysis reports", result.Error?.Description);

        var stored = await LoadAnalysisAsync(analysisId);
        Assert.NotNull(stored);
        Assert.False(stored.IsDeleted);
    }

    [Fact]
    public async Task Delete_EmptyActorAccountIdWithoutPrivileges_IsRestricted()
    {
        var analysisId = await SeedAsync(Guid.CreateVersion7());

        var result = await HandleAsync(
            new DeleteAnalysisReportCommand
            {
                AnalysisId = analysisId,
                ActorAccountId = Guid.Empty,
                ActorRole = "Doctor",
            }
        );

        Assert.True(result.IsFailure());
        Assert.Equal(ApplicationStatus.Restricted, result.StatusCode);
        Assert.Equal("You can only delete your own analysis reports", result.Error?.Description);
    }

    [Fact]
    public async Task Delete_ReportWithoutDoctor_PlainDoctorIsRestricted()
    {
        var analysisId = await SeedAsync(null);

        var result = await HandleAsync(
            new DeleteAnalysisReportCommand
            {
                AnalysisId = analysisId,
                ActorAccountId = Guid.CreateVersion7(),
                ActorRole = "Doctor",
            }
        );

        Assert.True(result.IsFailure());
        Assert.Equal(ApplicationStatus.Restricted, result.StatusCode);
        Assert.Equal("You can only delete your own analysis reports", result.Error?.Description);
    }

    [Fact]
    public async Task Delete_ReportWithoutDoctor_AdminSucceeds()
    {
        var analysisId = await SeedAsync(null);

        var result = await HandleAsync(
            new DeleteAnalysisReportCommand
            {
                AnalysisId = analysisId,
                ActorAccountId = Guid.CreateVersion7(),
                ActorRole = "Admin",
            }
        );

        Assert.True(result.IsSuccess());
        Assert.Equal(ApplicationStatus.Success, result.StatusCode);

        var stored = await LoadAnalysisAsync(analysisId);
        Assert.NotNull(stored);
        Assert.True(stored.IsDeleted);
    }

    [Fact]
    public async Task Delete_ReportWithoutDoctor_HeadDepartmentDoctorSucceeds()
    {
        var analysisId = await SeedAsync(null);

        var result = await HandleAsync(
            new DeleteAnalysisReportCommand
            {
                AnalysisId = analysisId,
                ActorAccountId = Guid.CreateVersion7(),
                ActorRole = "HeadDepartmentDoctor",
            }
        );

        Assert.True(result.IsSuccess());
        Assert.Equal(ApplicationStatus.Success, result.StatusCode);

        var stored = await LoadAnalysisAsync(analysisId);
        Assert.NotNull(stored);
        Assert.True(stored.IsDeleted);
    }

    [Fact]
    public async Task Delete_DeletedReport_IsExcludedFromFreshQuery()
    {
        var analysisId = await SeedAsync(Guid.CreateVersion7());

        var result = await HandleAsync(
            new DeleteAnalysisReportCommand
            {
                AnalysisId = analysisId,
                ActorAccountId = Guid.CreateVersion7(),
                ActorRole = "Admin",
            }
        );

        Assert.True(result.IsSuccess());

        await using var context = new AnalysisDbContext(_options);
        var queryHandler = new GetAnalysisReportQueryHandler(context);
        var queryResult = await queryHandler.HandleAsync(
            new GetAnalysisReportQuery { AnalysisId = analysisId },
            TestContext.Current.CancellationToken
        );

        Assert.True(queryResult.IsFailure());
        Assert.Equal(ApplicationStatus.ResourceNotFound, queryResult.StatusCode);
    }

    [Fact]
    public async Task Delete_ReturnsSuccessStatusAndAnalysisId()
    {
        var analysisId = await SeedAsync(Guid.CreateVersion7());

        var result = await HandleAsync(
            new DeleteAnalysisReportCommand
            {
                AnalysisId = analysisId,
                ActorAccountId = Guid.CreateVersion7(),
                ActorRole = "Admin",
            }
        );

        Assert.True(result.IsSuccess());
        Assert.Equal(ApplicationStatus.Success, result.StatusCode);
        Assert.Null(result.Error);
        Assert.NotNull(result.Data);
        Assert.Equal(analysisId, result.Data.AnalysisId);
    }

    [Fact]
    public async Task Delete_IsSoftDelete_RowStillExists()
    {
        var analysisId = await SeedAsync(Guid.CreateVersion7());

        var result = await HandleAsync(
            new DeleteAnalysisReportCommand
            {
                AnalysisId = analysisId,
                ActorAccountId = Guid.CreateVersion7(),
                ActorRole = "Admin",
            }
        );

        Assert.True(result.IsSuccess());

        var stored = await LoadAnalysisAsync(analysisId);
        Assert.NotNull(stored);
        Assert.True(stored.IsDeleted);
        Assert.NotNull(stored.DeletedAt);
    }

    private Task<Result<DeleteAnalysisReportResult>> HandleAsync(
        DeleteAnalysisReportCommand command
    ) => _handler.HandleAsync(command, TestContext.Current.CancellationToken);

    private async Task<Guid> SeedAsync(Guid? doctorId, bool softDeleted = false)
    {
        var analysisId = Guid.CreateVersion7();

        await using var context = new AnalysisDbContext(_options);
        context.ClinicalAnalyses.Add(
            new ClinicalAnalysis
            {
                Id = analysisId,
                DoctorId = doctorId,
                Start = Start,
                End = End,
                ReportJson = CreateReportJson(analysisId, doctorId),
                CreatedAt = OldTimestamp,
                UpdatedAt = OldTimestamp,
                IsDeleted = softDeleted,
                DeletedAt = softDeleted ? OldTimestamp : null,
            }
        );
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        return analysisId;
    }

    private async Task<ClinicalAnalysis?> LoadAnalysisAsync(Guid analysisId)
    {
        await using var context = new AnalysisDbContext(_options);
        return await context
            .ClinicalAnalyses.IgnoreQueryFilters()
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == analysisId, TestContext.Current.CancellationToken);
    }

    private static string CreateReportJson(Guid analysisId, Guid? doctorId)
    {
        var report = new AnalysisReportResult(
            analysisId,
            doctorId,
            Start,
            End,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            []
        );

        return JsonSerializer.Serialize(report, new JsonSerializerOptions(JsonSerializerDefaults.Web));
    }
}
