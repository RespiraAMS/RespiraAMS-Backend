using Application.Contracts.Data;
using Application.Features.IcuHospitalizeCriteria.CreateIcuHospitalizeCriterion;
using Application.Features.Shared.ManageCriterion;
using Domain.Enums;
using Domain.Models;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Range = Domain.Models.Range;
using Respira.ServiceDefaults.Contracts.Results;

namespace Application.Test.Features.IcuHospitalizeCriteria.CreateIcuHospitalizeCriterion;

public class CreateIcuHospitalizeCriterionHandlerTest : IClassFixture<PostgresFixture>, IAsyncLifetime
{
    private readonly DbContextOptions<AppDbContext> _options;
    private readonly CreateIcuHospitalizeCriterionHandler _handler;
    private readonly IDbContext _context;

    public CreateIcuHospitalizeCriterionHandlerTest(PostgresFixture fixture)
    {
        // Create handler dependencies
        _options = new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(fixture.ConnectionString).Options;
        _context = new AppDbContext(_options);
        var mapper = new CreateIcuHospitalizeCriterionMapper(new CreateCriterionMapper());
        var logger = new Mock<ILogger<CreateIcuHospitalizeCriterionHandler>>().Object;

        // Initialize handler
        _handler = new(_context, mapper, logger);
    }

    public async ValueTask DisposeAsync()
    {
        await _context.DisposeAsync();
    }

    public async ValueTask InitializeAsync()
    {
        // ICU criteria reference diseases through an FK, and each owns a Criterion
        // row (no DbSet is exposed for Criterion). Delete the owned criteria via raw
        // SQL first, then the ICU criteria and diseases. IgnoreQueryFilters is needed
        // because soft-deleted rows are hidden by the query filter but still occupy
        // the table
        var db = (AppDbContext)_context;
        await db.Database.ExecuteSqlRawAsync("DELETE FROM criteria",
            TestContext.Current.CancellationToken);
        await _context.IcuHospitalizeCriteria.IgnoreQueryFilters()
            .ExecuteDeleteAsync(TestContext.Current.CancellationToken);
        await _context.Diseases.IgnoreQueryFilters()
            .ExecuteDeleteAsync(TestContext.Current.CancellationToken);
    }

    private async Task<Guid> SeedDiseaseAsync(bool softDeleted = false)
    {
        var disease = new Disease
        {
            Name = "Community-acquired pneumonia",
            Description = "Acute lung infection acquired outside of healthcare settings",
            // CURB-65 ICU threshold: >= 3 recommends ICU admission
            IcuScoreThreshold = 3,
            IsDeleted = softDeleted,
            DeletedAt = softDeleted ? DateTimeOffset.UtcNow : null,
        };
        await _context.Diseases.AddAsync(disease, TestContext.Current.CancellationToken);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);
        return disease.Id;
    }

    private async Task<IcuHospitalizeCriterion> GetPersistedAsync(Guid id)
    {
        await using var freshContext = new AppDbContext(_options);
        return await freshContext.IcuHospitalizeCriteria.IgnoreQueryFilters()
            .Include(x => x.Criterion)
            .SingleAsync(x => x.Id == id, TestContext.Current.CancellationToken);
    }

    # region Happy path

    [Fact]
    public async Task CreateIcuHospitalizeCriterion_BooleanType_Success()
    {
        var diseaseId = await SeedDiseaseAsync();

        var result = await _handler.HandleAsync(new CreateIcuHospitalizeCriterionCommand
        {
            DiseaseId = diseaseId,
            Criterion = new CreateCriterionCommand
            {
                Name = "Altered mental status",
                Type = CriterionType.Boolean,
                Value = null,
            },
            // NEWS2: new confusion scores 1 point
            Score = 1,
        }, TestContext.Current.CancellationToken);
        Assert.True(result.IsSuccess());
        Assert.Null(result.Error);
        Assert.Equal(Status.Created, result.StatusCode);
        Assert.NotNull(result.Data);

        Assert.NotEqual(Guid.Empty, result.Data.Id);

        // Verify through a fresh context so the change tracker cannot mask a failed commit
        var saved = await GetPersistedAsync(result.Data.Id);
        Assert.Equal(diseaseId, saved.DiseaseId);
        Assert.Equal(1, saved.Score);
        Assert.Equal(saved.CriterionId, saved.Criterion.Id);
        Assert.Equal("Altered mental status", saved.Criterion.Name);
        Assert.IsType<BooleanCriterion>(saved.Criterion);

        // The owned Criterion row must have been persisted too
        await using var freshContext = new AppDbContext(_options);
        Assert.True(await freshContext.Set<Criterion>().IgnoreQueryFilters()
            .AnyAsync(x => x.Id == saved.CriterionId, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task CreateIcuHospitalizeCriterion_NumericType_Success()
    {
        // Respiratory rate >= 20 breaths/min (3 NEWS2 points) with no upper bound
        var diseaseId = await SeedDiseaseAsync();
        var value = new Range
        {
            Min = 20,
            IsMinExclusive = false,
            Max = decimal.MaxValue,
            IsMaxExclusive = false,
            Unit = "breaths/min",
        };

        var result = await _handler.HandleAsync(new CreateIcuHospitalizeCriterionCommand
        {
            DiseaseId = diseaseId,
            Criterion = new CreateCriterionCommand
            {
                Name = "Respiratory rate",
                Type = CriterionType.Numeric,
                Value = value,
            },
            Score = 3,
        }, TestContext.Current.CancellationToken);
        Assert.True(result.IsSuccess());
        Assert.Null(result.Error);
        Assert.Equal(Status.Created, result.StatusCode);
        Assert.NotNull(result.Data);

        var saved = await GetPersistedAsync(result.Data.Id);
        Assert.Equal(diseaseId, saved.DiseaseId);
        Assert.Equal(3, saved.Score);
        var numeric = Assert.IsType<NumericCriterion>(saved.Criterion);
        Assert.Equal(20, numeric.Value.Min);
        Assert.Equal(decimal.MaxValue, numeric.Value.Max);
        Assert.Equal("breaths/min", numeric.Value.Unit);
    }

    # endregion

    # region Fail path

    [Fact]
    public async Task CreateIcuHospitalizeCriterion_DiseaseNotFound_Fail()
    {
        var unknownId = Guid.CreateVersion7();

        var result = await _handler.HandleAsync(
            new CreateIcuHospitalizeCriterionCommand
            {
                DiseaseId = unknownId,
                Criterion = new CreateCriterionCommand
                {
                    Name = "Altered mental status",
                    Type = CriterionType.Boolean,
                    Value = null,
                },
                Score = 1,
            }, TestContext.Current.CancellationToken);
        Assert.True(result.IsFailure());
        Assert.NotNull(result.Error);
        Assert.Equal(Status.BadRequest, result.StatusCode);

        // Neither the ICU criterion nor its owned Criterion row must be persisted
        Assert.Equal(0, await _context.IcuHospitalizeCriteria.IgnoreQueryFilters()
            .CountAsync(TestContext.Current.CancellationToken));
        await using var freshContext = new AppDbContext(_options);
        Assert.Equal(0, await freshContext.Set<Criterion>().IgnoreQueryFilters()
            .CountAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task CreateIcuHospitalizeCriterion_SoftDeletedDisease_Fail()
    {
        // A soft-deleted disease is hidden by the query filter, so referencing it
        // must be rejected just like an unknown disease
        var deletedDiseaseId = await SeedDiseaseAsync(softDeleted: true);

        var result = await _handler.HandleAsync(
            new CreateIcuHospitalizeCriterionCommand
            {
                DiseaseId = deletedDiseaseId,
                Criterion = new CreateCriterionCommand
                {
                    Name = "Altered mental status",
                    Type = CriterionType.Boolean,
                    Value = null,
                },
                Score = 1,
            }, TestContext.Current.CancellationToken);
        Assert.True(result.IsFailure());
        Assert.NotNull(result.Error);
        Assert.Equal(Status.BadRequest, result.StatusCode);
    }

    # endregion
}
