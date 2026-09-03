using Application.Contracts.Data;
using Application.Features.ResistanceRiskFactors.DeleteResistanceRiskFactor;
using Domain.Models;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Range = Domain.Models.Range;
using Respira.ServiceDefaults.Contracts.Results;

namespace Application.Test.Features.ResistanceRiskFactors.DeleteResistanceRiskFactor;

public class DeleteResistanceRiskFactorHandlerTest : IClassFixture<PostgresFixture>, IAsyncLifetime
{
    private readonly DbContextOptions<AppDbContext> _options;
    private readonly DeleteResistanceRiskFactorHandler _handler;
    private readonly IDbContext _context;

    public DeleteResistanceRiskFactorHandlerTest(PostgresFixture fixture)
    {
        // Create handler dependencies
        _options = new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(fixture.ConnectionString).Options;
        _context = new AppDbContext(_options);
        var logger = new Mock<ILogger<DeleteResistanceRiskFactorHandler>>().Object;

        // Initialize handler
        _handler = new(_context, logger);
    }

    public async ValueTask DisposeAsync()
    {
        await _context.DisposeAsync();
    }

    public async ValueTask InitializeAsync()
    {
        // A resistance risk factor owns a Criterion row (no DbSet is exposed for
        // Criterion) and references Disease and Pathogen. Delete children/owned rows
        // first, then parents. IgnoreQueryFilters is needed because soft-deleted rows
        // are hidden by the query filter but still occupy the table.
        await _context.ResistanceRiskFactors.IgnoreQueryFilters()
            .ExecuteDeleteAsync(TestContext.Current.CancellationToken);
        await ((AppDbContext)_context).Database.ExecuteSqlRawAsync("DELETE FROM criteria",
            TestContext.Current.CancellationToken);
        await _context.Pathogens.IgnoreQueryFilters()
            .ExecuteDeleteAsync(TestContext.Current.CancellationToken);
        await _context.Diseases.IgnoreQueryFilters()
            .ExecuteDeleteAsync(TestContext.Current.CancellationToken);
    }

    private async Task<Guid> SeedDiseaseAsync()
    {
        var disease = new Disease
        {
            Name = "Hospital-acquired pneumonia",
            Description = "Pneumonia occurring more than 48h after hospital admission",
            IcuScoreThreshold = 3,
        };
        await _context.Diseases.AddAsync(disease, TestContext.Current.CancellationToken);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);
        return disease.Id;
    }

    private async Task<Guid> SeedPathogenAsync()
    {
        var pathogen = new Pathogen
        {
            Name = "Pseudomonas aeruginosa",
            Description = "Multidrug-resistant Gram-negative bacillus",
        };
        await _context.Pathogens.AddAsync(pathogen, TestContext.Current.CancellationToken);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);
        return pathogen.Id;
    }

    private async Task<Guid> SeedFactorAsync(Guid diseaseId, Guid pathogenId, bool numeric, bool softDeletedFactor = false)
    {
        var criterion = numeric
            ? new NumericCriterion
            {
                Name = "WBC count",
                // Realistic boundary: WBC >= 12 x10^9/L encodes no upper bound
                Value = new Range { Min = 12, IsMinExclusive = false, Max = decimal.MaxValue, IsMaxExclusive = false, Unit = "x10^9/L" },
            }
            : new BooleanCriterion { Name = "Prior antibiotic use" } as Criterion;

        var factor = new ResistanceRiskFactor
        {
            DiseaseId = diseaseId,
            PathogenId = pathogenId,
            Name = "Prior antibiotic use in last 90 days",
            CriterionId = criterion.Id,
            Criterion = criterion,
            IsDeleted = softDeletedFactor,
            DeletedAt = softDeletedFactor ? DateTimeOffset.UtcNow : null,
        };
        await _context.ResistanceRiskFactors.AddAsync(factor, TestContext.Current.CancellationToken);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);
        return factor.Id;
    }

    private async Task<ResistanceRiskFactor?> GetPersistedAsync(Guid id, bool ignoreFilters = true)
    {
        await using var freshContext = new AppDbContext(_options);
        var query = freshContext.ResistanceRiskFactors;
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
    public async Task DeleteResistanceRiskFactor_BooleanType_Success()
    {
        var diseaseId = await SeedDiseaseAsync();
        var pathogenId = await SeedPathogenAsync();
        var factorId = await SeedFactorAsync(diseaseId, pathogenId, numeric: false);

        var result = await _handler.HandleAsync(new DeleteResistanceRiskFactorCommand
        {
            Id = factorId,
        }, TestContext.Current.CancellationToken);
        Assert.True(result.IsSuccess);
        Assert.Null(result.Error);
        Assert.Equal(Status.Deleted, result.StatusCode);

        // The row must be soft-deleted: hidden by the normal query filter
        Assert.Null(await GetPersistedAsync(factorId, ignoreFilters: false));

        // ...but still present (and flagged) at the storage level
        var saved = await GetPersistedAsync(factorId);
        Assert.NotNull(saved);
        Assert.True(saved.IsDeleted);
        Assert.InRange(saved.DeletedAt ?? DateTimeOffset.MinValue, DateTimeOffset.UtcNow.AddMinutes(-1), DateTimeOffset.UtcNow.AddSeconds(5));

        // IMPORTANT: the handler only soft-deletes the factor row; the owned Criterion
        // row is left untouched (orphaned). This mirrors Create/Update behavior.
        await using var freshContext = new AppDbContext(_options);
        Assert.True(await freshContext.Set<Criterion>().IgnoreQueryFilters()
            .AnyAsync(x => x.Id == saved.CriterionId, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task DeleteResistanceRiskFactor_NumericType_Success()
    {
        var diseaseId = await SeedDiseaseAsync();
        var pathogenId = await SeedPathogenAsync();
        var factorId = await SeedFactorAsync(diseaseId, pathogenId, numeric: true);

        var result = await _handler.HandleAsync(new DeleteResistanceRiskFactorCommand
        {
            Id = factorId,
        }, TestContext.Current.CancellationToken);
        Assert.True(result.IsSuccess);
        Assert.Null(result.Error);
        Assert.Equal(Status.Deleted, result.StatusCode);

        Assert.Null(await GetPersistedAsync(factorId, ignoreFilters: false));
        var saved = await GetPersistedAsync(factorId);
        Assert.NotNull(saved);
        Assert.True(saved.IsDeleted);
    }

    # endregion

    # region Fail path

    [Fact]
    public async Task DeleteResistanceRiskFactor_NotFound_Fail()
    {
        var unknownId = Guid.CreateVersion7();

        var result = await _handler.HandleAsync(
            new DeleteResistanceRiskFactorCommand
            {
                Id = unknownId,
            }, TestContext.Current.CancellationToken);
        Assert.True(result.IsFailure);
        Assert.NotNull(result.Error);
        Assert.Equal(Status.BadRequest, result.StatusCode);
    }

    [Fact]
    public async Task DeleteResistanceRiskFactor_AlreadySoftDeleted_Fail()
    {
        // A soft-deleted factor is hidden by the query filter and must be treated as
        // not found on a second delete attempt
        var diseaseId = await SeedDiseaseAsync();
        var pathogenId = await SeedPathogenAsync();
        var factorId = await SeedFactorAsync(diseaseId, pathogenId, numeric: false, softDeletedFactor: true);

        var result = await _handler.HandleAsync(
            new DeleteResistanceRiskFactorCommand
            {
                Id = factorId,
            }, TestContext.Current.CancellationToken);
        Assert.True(result.IsFailure);
        Assert.NotNull(result.Error);
        Assert.Equal(Status.BadRequest, result.StatusCode);
    }

    # endregion
}
