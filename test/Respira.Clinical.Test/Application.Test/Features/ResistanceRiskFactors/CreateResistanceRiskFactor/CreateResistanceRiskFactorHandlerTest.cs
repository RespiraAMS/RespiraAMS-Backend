using Application.Contracts.Data;
using Application.Features.ResistanceRiskFactors.CreateResistanceRiskFactor;
using Application.Features.Shared.ManageCriterion;
using Domain.Enums;
using Domain.Models;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Range = Domain.Models.Range;
using Respira.ServiceDefaults.Contracts.Results;

namespace Application.Test.Features.ResistanceRiskFactors.CreateResistanceRiskFactor;

public class CreateResistanceRiskFactorHandlerTest : IClassFixture<PostgresFixture>, IAsyncLifetime
{
    private readonly DbContextOptions<AppDbContext> _options;
    private readonly CreateResistanceRiskFactorHandler _handler;
    private readonly IDbContext _context;

    public CreateResistanceRiskFactorHandlerTest(PostgresFixture fixture)
    {
        // Create handler dependencies
        _options = new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(fixture.ConnectionString).Options;
        _context = new AppDbContext(_options);
        var mapper = new CreateResistanceRiskFactorMapper(new CreateCriterionMapper());
        var logger = new Mock<ILogger<CreateResistanceRiskFactorCommand>>().Object;

        // Initialize handler
        _handler = new(_context, mapper, logger);
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

    private async Task<Guid> SeedDiseaseAsync(bool softDeleted = false)
    {
        var disease = new Disease
        {
            Name = "Hospital-acquired pneumonia",
            Description = "Pneumonia occurring more than 48h after hospital admission",
            IcuScoreThreshold = 3,
            IsDeleted = softDeleted,
            DeletedAt = softDeleted ? DateTimeOffset.UtcNow : null,
        };
        await _context.Diseases.AddAsync(disease, TestContext.Current.CancellationToken);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);
        return disease.Id;
    }

    private async Task<Guid> SeedPathogenAsync(bool softDeleted = false)
    {
        var pathogen = new Pathogen
        {
            Name = "Pseudomonas aeruginosa",
            Description = "Multidrug-resistant Gram-negative bacillus",
            IsDeleted = softDeleted,
            DeletedAt = softDeleted ? DateTimeOffset.UtcNow : null,
        };
        await _context.Pathogens.AddAsync(pathogen, TestContext.Current.CancellationToken);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);
        return pathogen.Id;
    }

    private async Task<ResistanceRiskFactor> GetPersistedAsync(Guid id)
    {
        await using var freshContext = new AppDbContext(_options);
        return await freshContext.ResistanceRiskFactors.IgnoreQueryFilters()
            .Include(x => x.Criterion)
            .SingleAsync(x => x.Id == id, TestContext.Current.CancellationToken);
    }

    # region Happy path

    [Fact]
    public async Task CreateResistanceRiskFactor_BooleanType_Success()
    {
        var diseaseId = await SeedDiseaseAsync();
        var pathogenId = await SeedPathogenAsync();

        var result = await _handler.HandleAsync(new CreateResistanceRiskFactorCommand
        {
            DiseaseId = diseaseId,
            PathogenId = pathogenId,
            Name = "Prior antibiotic use in last 90 days",
            Criterion = new CreateCriterionCommand
            {
                Name = "Prior antibiotic use",
                Type = CriterionType.Boolean,
                Value = null,
            },
        }, TestContext.Current.CancellationToken);
        Assert.True(result.IsSuccess);
        Assert.Null(result.Error);
        Assert.Equal(Status.Created, result.StatusCode);
        Assert.NotNull(result.Data);

        Assert.NotEqual(Guid.Empty, result.Data.Id);

        // Verify through a fresh context so the change tracker cannot mask a failed commit
        var saved = await GetPersistedAsync(result.Data.Id);
        Assert.Equal(diseaseId, saved.DiseaseId);
        Assert.Equal(pathogenId, saved.PathogenId);
        Assert.Equal("Prior antibiotic use in last 90 days", saved.Name);
        Assert.Equal(saved.CriterionId, saved.Criterion.Id);
        Assert.IsType<BooleanCriterion>(saved.Criterion);

        // The owned Criterion row must have been persisted too
        await using var freshContext = new AppDbContext(_options);
        Assert.True(await freshContext.Set<Criterion>().IgnoreQueryFilters()
            .AnyAsync(x => x.Id == saved.CriterionId, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task CreateResistanceRiskFactor_NumericType_Success()
    {
        // WBC count >= 12 x10^9/L (realistic neutrophil-predominant infection band)
        var diseaseId = await SeedDiseaseAsync();
        var pathogenId = await SeedPathogenAsync();
        var value = new Range
        {
            Min = 12,
            IsMinExclusive = false,
            Max = decimal.MaxValue,
            IsMaxExclusive = false,
            Unit = "x10^9/L",
        };

        var result = await _handler.HandleAsync(new CreateResistanceRiskFactorCommand
        {
            DiseaseId = diseaseId,
            PathogenId = pathogenId,
            Name = "Elevated WBC count",
            Criterion = new CreateCriterionCommand
            {
                Name = "WBC count",
                Type = CriterionType.Numeric,
                Value = value,
            },
        }, TestContext.Current.CancellationToken);
        Assert.True(result.IsSuccess);
        Assert.Null(result.Error);
        Assert.Equal(Status.Created, result.StatusCode);
        Assert.NotNull(result.Data);

        var saved = await GetPersistedAsync(result.Data.Id);
        Assert.Equal(diseaseId, saved.DiseaseId);
        Assert.Equal(pathogenId, saved.PathogenId);
        Assert.Equal("Elevated WBC count", saved.Name);
        var numeric = Assert.IsType<NumericCriterion>(saved.Criterion);
        Assert.Equal(12, numeric.Value.Min);
        Assert.Equal(decimal.MaxValue, numeric.Value.Max);
        Assert.Equal("x10^9/L", numeric.Value.Unit);
    }

    # endregion

    # region Fail path

    [Fact]
    public async Task CreateResistanceRiskFactor_DiseaseNotFound_Fail()
    {
        var pathogenId = await SeedPathogenAsync();

        var result = await _handler.HandleAsync(
            new CreateResistanceRiskFactorCommand
            {
                DiseaseId = Guid.CreateVersion7(),
                PathogenId = pathogenId,
                Name = "Prior antibiotic use",
                Criterion = new CreateCriterionCommand
                {
                    Name = "Prior antibiotic use",
                    Type = CriterionType.Boolean,
                    Value = null,
                },
            }, TestContext.Current.CancellationToken);
        Assert.True(result.IsFailure);
        Assert.NotNull(result.Error);
        Assert.Equal(Status.BadRequest, result.StatusCode);
    }

    [Fact]
    public async Task CreateResistanceRiskFactor_PathogenNotFound_Fail()
    {
        var diseaseId = await SeedDiseaseAsync();

        var result = await _handler.HandleAsync(
            new CreateResistanceRiskFactorCommand
            {
                DiseaseId = diseaseId,
                PathogenId = Guid.CreateVersion7(),
                Name = "Prior antibiotic use",
                Criterion = new CreateCriterionCommand
                {
                    Name = "Prior antibiotic use",
                    Type = CriterionType.Boolean,
                    Value = null,
                },
            }, TestContext.Current.CancellationToken);
        Assert.True(result.IsFailure);
        Assert.NotNull(result.Error);
        Assert.Equal(Status.BadRequest, result.StatusCode);
    }

    [Fact]
    public async Task CreateResistanceRiskFactor_SoftDeletedDisease_Fail()
    {
        var diseaseId = await SeedDiseaseAsync(softDeleted: true);
        var pathogenId = await SeedPathogenAsync();

        var result = await _handler.HandleAsync(
            new CreateResistanceRiskFactorCommand
            {
                DiseaseId = diseaseId,
                PathogenId = pathogenId,
                Name = "Prior antibiotic use",
                Criterion = new CreateCriterionCommand
                {
                    Name = "Prior antibiotic use",
                    Type = CriterionType.Boolean,
                    Value = null,
                },
            }, TestContext.Current.CancellationToken);
        Assert.True(result.IsFailure);
        Assert.NotNull(result.Error);
        Assert.Equal(Status.BadRequest, result.StatusCode);
    }

    [Fact]
    public async Task CreateResistanceRiskFactor_SoftDeletedPathogen_Fail()
    {
        var diseaseId = await SeedDiseaseAsync();
        var pathogenId = await SeedPathogenAsync(softDeleted: true);

        var result = await _handler.HandleAsync(
            new CreateResistanceRiskFactorCommand
            {
                DiseaseId = diseaseId,
                PathogenId = pathogenId,
                Name = "Prior antibiotic use",
                Criterion = new CreateCriterionCommand
                {
                    Name = "Prior antibiotic use",
                    Type = CriterionType.Boolean,
                    Value = null,
                },
            }, TestContext.Current.CancellationToken);
        Assert.True(result.IsFailure);
        Assert.NotNull(result.Error);
        Assert.Equal(Status.BadRequest, result.StatusCode);
    }

    # endregion
}
