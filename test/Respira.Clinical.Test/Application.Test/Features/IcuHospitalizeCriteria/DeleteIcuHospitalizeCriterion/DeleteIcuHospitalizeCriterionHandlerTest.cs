using Application.Contracts.Data;
using Application.Features.IcuHospitalizeCriteria.DeleteIcuHospitalizeCriterion;
using Domain.Models;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Range = Domain.Models.Range;
using Respira.ServiceDefaults.Contracts.Results;

namespace Application.Test.Features.IcuHospitalizeCriteria.DeleteIcuHospitalizeCriterion;

public class DeleteIcuHospitalizeCriterionHandlerTest : IClassFixture<PostgresFixture>, IAsyncLifetime
{
    private readonly DbContextOptions<AppDbContext> _options;
    private readonly DeleteIcuHospitalizeCriterionHandler _handler;
    private readonly IDbContext _context;

    public DeleteIcuHospitalizeCriterionHandlerTest(PostgresFixture fixture)
    {
        // Create handler dependencies
        _options = new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(fixture.ConnectionString).Options;
        _context = new AppDbContext(_options);
        var logger = new Mock<ILogger<DeleteIcuHospitalizeCriterionHandler>>().Object;

        // Initialize handler
        _handler = new(_context, logger);
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

    private async Task<Guid> SeedDiseaseAsync()
    {
        var disease = new Disease
        {
            Name = "Community-acquired pneumonia",
            Description = "Acute lung infection acquired outside of healthcare settings",
            // CURB-65 ICU threshold: >= 3 recommends ICU admission
            IcuScoreThreshold = 3,
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
                // Realistic boundary: RR >= 20 breaths/min encodes no upper bound
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

    private async Task<IcuHospitalizeCriterion?> GetPersistedAsync(Guid id, bool ignoreFilters = true)
    {
        await using var freshContext = new AppDbContext(_options);
        var query = freshContext.IcuHospitalizeCriteria;
        if (ignoreFilters)
        {
            return await query.IgnoreQueryFilters()
                .Include(x => x.Criterion)
                .SingleOrDefaultAsync(x => x.Id == id, TestContext.Current.CancellationToken);
        }
        return await query
            .Include(x => x.Criterion)
            .SingleOrDefaultAsync(x => x.Id == id, TestContext.Current.CancellationToken);
    }

    # region Happy path

    [Fact]
    public async Task DeleteIcuHospitalizeCriterion_BooleanType_Success()
    {
        var diseaseId = await SeedDiseaseAsync();
        var icuId = await SeedIcuCriterionAsync(diseaseId, numeric: false);

        var result = await _handler.HandleAsync(new DeleteIcuHospitalizeCriterionCommand
        {
            Id = icuId,
        }, TestContext.Current.CancellationToken);
        Assert.True(result.IsSuccess);
        Assert.Null(result.Error);
        Assert.Equal(Status.Deleted, result.StatusCode);

        // The row must be soft-deleted: hidden by the normal query filter
        Assert.Null(await GetPersistedAsync(icuId, ignoreFilters: false));

        // ...but still present (and flagged) at the storage level
        var saved = await GetPersistedAsync(icuId);
        Assert.NotNull(saved);
        Assert.True(saved.IsDeleted);
        Assert.InRange(saved.DeletedAt ?? DateTimeOffset.MinValue, DateTimeOffset.UtcNow.AddMinutes(-1), DateTimeOffset.UtcNow.AddSeconds(5));

        // IMPORTANT: the handler only soft-deletes the ICU row; the owned Criterion
        // row is left untouched (orphaned). This mirrors Create/Update behavior.
        await using var freshContext = new AppDbContext(_options);
        Assert.True(await freshContext.Set<Criterion>().IgnoreQueryFilters()
            .AnyAsync(x => x.Id == saved.CriterionId, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task DeleteIcuHospitalizeCriterion_NumericType_Success()
    {
        var diseaseId = await SeedDiseaseAsync();
        var icuId = await SeedIcuCriterionAsync(diseaseId, numeric: true);

        var result = await _handler.HandleAsync(new DeleteIcuHospitalizeCriterionCommand
        {
            Id = icuId,
        }, TestContext.Current.CancellationToken);
        Assert.True(result.IsSuccess);
        Assert.Null(result.Error);
        Assert.Equal(Status.Deleted, result.StatusCode);

        Assert.Null(await GetPersistedAsync(icuId, ignoreFilters: false));
        var saved = await GetPersistedAsync(icuId);
        Assert.NotNull(saved);
        Assert.True(saved.IsDeleted);
    }

    # endregion

    # region Fail path

    [Fact]
    public async Task DeleteIcuHospitalizeCriterion_NotFound_Fail()
    {
        var unknownId = Guid.CreateVersion7();

        var result = await _handler.HandleAsync(
            new DeleteIcuHospitalizeCriterionCommand
            {
                Id = unknownId,
            }, TestContext.Current.CancellationToken);
        Assert.True(result.IsFailure);
        Assert.NotNull(result.Error);
        Assert.Equal(Status.BadRequest, result.StatusCode);
    }

    [Fact]
    public async Task DeleteIcuHospitalizeCriterion_AlreadySoftDeleted_Fail()
    {
        // A soft-deleted ICU criterion is hidden by the query filter and must be
        // treated as not found on a second delete attempt
        var diseaseId = await SeedDiseaseAsync();
        var icuId = await SeedIcuCriterionAsync(diseaseId, numeric: false, softDeletedIcu: true);

        var result = await _handler.HandleAsync(
            new DeleteIcuHospitalizeCriterionCommand
            {
                Id = icuId,
            }, TestContext.Current.CancellationToken);
        Assert.True(result.IsFailure);
        Assert.NotNull(result.Error);
        Assert.Equal(Status.BadRequest, result.StatusCode);
    }

    # endregion
}
