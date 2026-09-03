using Application.Contracts.Data;
using Application.Features.IcuHospitalizeCriteria.UpdateIcuHospitalizeCriterion;
using Application.Features.Shared.ManageCriterion;
using Domain.Enums;
using Domain.Models;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Range = Domain.Models.Range;
using Respira.ServiceDefaults.Contracts.Results;

namespace Application.Test.Features.IcuHospitalizeCriteria.UpdateIcuHospitalizeCriterion;

public class UpdateIcuHospitalizeCriterionHandlerTest : IClassFixture<PostgresFixture>, IAsyncLifetime
{
    private readonly DbContextOptions<AppDbContext> _options;
    private readonly UpdateIcuHospitalizeCriterionHandler _handler;
    private readonly IDbContext _context;

    public UpdateIcuHospitalizeCriterionHandlerTest(PostgresFixture fixture)
    {
        // Create handler dependencies
        _options = new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(fixture.ConnectionString).Options;
        _context = new AppDbContext(_options);
        var mapper = new UpdateIcuHospitalizeCriterionMapper(new UpdateCriterionMapper());
        var logger = new Mock<ILogger<UpdateIcuHospitalizeCriterionHandler>>().Object;

        // Initialize handler
        _handler = new(_context, mapper, logger);
    }

    public async ValueTask DisposeAsync()
    {
        await _context.DisposeAsync();
    }

    public async ValueTask InitializeAsync()
    {
        // An ICU criterion owns a Criterion row (no DbSet is exposed for Criterion).
        // Delete the owned criteria via raw SQL first, then the ICU criteria and
        // diseases. IgnoreQueryFilters is needed because soft-deleted rows are hidden
        // by the query filter but still occupy the table.
        await ((AppDbContext)_context).Database.ExecuteSqlRawAsync("DELETE FROM criteria",
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

    private async Task<Guid> SeedIcuCriterionAsync(Guid diseaseId, bool numeric, bool softDeletedIcu = false)
    {
        var criterion = numeric
            ? new NumericCriterion
            {
                Name = "Respiratory rate",
                Value = new Range { Min = 20, IsMinExclusive = false, Max = decimal.MaxValue, IsMaxExclusive = false, Unit = "breaths/min" },
            }
            : new BooleanCriterion { Name = "Altered mental status" } as Criterion;

        var icu = new IcuHospitalizeCriterion
        {
            DiseaseId = diseaseId,
            CriterionId = criterion.Id,
            Criterion = criterion,
            Score = 1,
            IsDeleted = softDeletedIcu,
            DeletedAt = softDeletedIcu ? DateTimeOffset.UtcNow : null,
        };
        await _context.IcuHospitalizeCriteria.AddAsync(icu, TestContext.Current.CancellationToken);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);
        return icu.Id;
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
    public async Task UpdateIcuHospitalizeCriterion_BooleanType_Success()
    {
        var diseaseId = await SeedDiseaseAsync();
        var icuId = await SeedIcuCriterionAsync(diseaseId, numeric: false);

        var result = await _handler.HandleAsync(new UpdateIcuHospitalizeCriterionCommand
        {
            Id = icuId,
            Criterion = new UpdateCriterionCommand
            {
                Name = "Confusion",
                Type = CriterionType.Boolean,
                Value = null,
            },
            // Update score from 1 to 2 (still a valid NEWS2 contribution)
            Score = 2,
        }, TestContext.Current.CancellationToken);
        Assert.True(result.IsSuccess);
        Assert.Null(result.Error);
        Assert.Equal(Status.Updated, result.StatusCode);

        // Verify through a fresh context so the change tracker cannot mask a failed commit
        var saved = await GetPersistedAsync(icuId);
        Assert.Equal(2, saved.Score);
        Assert.Equal("Confusion", saved.Criterion.Name);
        Assert.IsType<BooleanCriterion>(saved.Criterion);
    }

    [Fact]
    public async Task UpdateIcuHospitalizeCriterion_NumericType_Success()
    {
        // Replace respiratory rate criterion with a systolic BP < 90 mmHg band
        var diseaseId = await SeedDiseaseAsync();
        var icuId = await SeedIcuCriterionAsync(diseaseId, numeric: true);
        var updated = new Range { Min = decimal.MinValue, IsMinExclusive = false, Max = 90, IsMaxExclusive = false, Unit = "mmHg" };

        var result = await _handler.HandleAsync(new UpdateIcuHospitalizeCriterionCommand
        {
            Id = icuId,
            Criterion = new UpdateCriterionCommand
            {
                Name = "Systolic blood pressure",
                Type = CriterionType.Numeric,
                Value = updated,
            },
            Score = 3,
        }, TestContext.Current.CancellationToken);
        Assert.True(result.IsSuccess);
        Assert.Null(result.Error);
        Assert.Equal(Status.Updated, result.StatusCode);

        var saved = await GetPersistedAsync(icuId);
        Assert.Equal(3, saved.Score);
        var numeric = Assert.IsType<NumericCriterion>(saved.Criterion);
        Assert.Equal("Systolic blood pressure", numeric.Name);
        Assert.Equal(decimal.MinValue, numeric.Value.Min);
        Assert.Equal(90, numeric.Value.Max);
        Assert.Equal("mmHg", numeric.Value.Unit);
    }

    # endregion

    # region Fail path

    [Fact]
    public async Task UpdateIcuHospitalizeCriterion_NotFound_Fail()
    {
        var unknownId = Guid.CreateVersion7();

        var result = await _handler.HandleAsync(
            new UpdateIcuHospitalizeCriterionCommand
            {
                Id = unknownId,
                Criterion = new UpdateCriterionCommand
                {
                    Name = "Confusion",
                    Type = CriterionType.Boolean,
                    Value = null,
                },
                Score = 1,
            }, TestContext.Current.CancellationToken);
        Assert.True(result.IsFailure);
        Assert.NotNull(result.Error);
        Assert.Equal(Status.BadRequest, result.StatusCode);
    }

    [Fact]
    public async Task UpdateIcuHospitalizeCriterion_SoftDeleted_Fail()
    {
        // A soft-deleted ICU criterion is hidden by the query filter and must be
        // treated as not found
        var diseaseId = await SeedDiseaseAsync();
        var icuId = await SeedIcuCriterionAsync(diseaseId, numeric: false, softDeletedIcu: true);

        var result = await _handler.HandleAsync(
            new UpdateIcuHospitalizeCriterionCommand
            {
                Id = icuId,
                Criterion = new UpdateCriterionCommand
                {
                    Name = "Confusion",
                    Type = CriterionType.Boolean,
                    Value = null,
                },
                Score = 1,
            }, TestContext.Current.CancellationToken);
        Assert.True(result.IsFailure);
        Assert.NotNull(result.Error);
        Assert.Equal(Status.BadRequest, result.StatusCode);
    }

    # endregion
}
