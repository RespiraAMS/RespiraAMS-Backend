using Application.Contracts.Data;
using Application.Features.ResistanceRiskFactors.UpdateResistanceRiskFactor;
using Application.Features.Shared.ManageCriterion;
using Domain.Enums;
using Domain.Models;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Range = Domain.Models.Range;
using Respira.ServiceDefaults.Exceptions;

namespace Application.Test.Features.ResistanceRiskFactors.UpdateResistanceRiskFactor;

public class UpdateResistanceRiskFactorHandlerTest : IClassFixture<PostgresFixture>, IAsyncLifetime
{
    private readonly DbContextOptions<AppDbContext> _options;
    private readonly UpdateResistanceRiskFactorHandler _handler;
    private readonly IDbContext _context;

    public UpdateResistanceRiskFactorHandlerTest(PostgresFixture fixture)
    {
        // Create handler dependencies
        _options = new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(fixture.ConnectionString).Options;
        _context = new AppDbContext(_options);
        var mapper = new UpdateResistanceRiskFactorMapper(new UpdateCriterionMapper());
        var logger = new Mock<ILogger<UpdateResistanceRiskFactorCommand>>().Object;

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

    private async Task<Guid> SeedFactorAsync(Guid diseaseId, Guid pathogenId, bool numeric, bool softDeletedFactor = false)
    {
        var criterion = numeric
            ? new NumericCriterion
            {
                Name = "WBC count",
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

    private async Task<ResistanceRiskFactor> GetPersistedAsync(Guid id)
    {
        await using var freshContext = new AppDbContext(_options);
        return await freshContext.ResistanceRiskFactors.IgnoreQueryFilters()
            .Include(x => x.Criterion)
            .SingleAsync(x => x.Id == id, TestContext.Current.CancellationToken);
    }

    # region Happy path

    [Fact]
    public async Task UpdateResistanceRiskFactor_BooleanType_Success()
    {
        var diseaseId = await SeedDiseaseAsync();
        var oldPathogenId = await SeedPathogenAsync();
        var newPathogenId = await SeedPathogenAsync();
        var factorId = await SeedFactorAsync(diseaseId, oldPathogenId, numeric: false);

        await _handler.HandleAsync(new UpdateResistanceRiskFactorCommand
        {
            Id = factorId,
            PathogenId = newPathogenId,
            Name = "Prior antibiotic use in last 90 days (updated)",
            Criterion = new UpdateCriterionCommand
            {
                Name = "Prior antibiotic use (updated)",
                Type = CriterionType.Boolean,
                Value = null,
            },
        }, TestContext.Current.CancellationToken);

        // Verify through a fresh context so the change tracker cannot mask a failed commit
        var saved = await GetPersistedAsync(factorId);
        Assert.Equal("Prior antibiotic use in last 90 days (updated)", saved.Name);
        Assert.Equal(newPathogenId, saved.PathogenId);
        Assert.Equal("Prior antibiotic use (updated)", saved.Criterion.Name);
        Assert.IsType<BooleanCriterion>(saved.Criterion);
    }

    [Fact]
    public async Task UpdateResistanceRiskFactor_NumericType_Success()
    {
        // Replace WBC criterion with CRP >= 100 mg/L band
        var diseaseId = await SeedDiseaseAsync();
        var pathogenId = await SeedPathogenAsync();
        var factorId = await SeedFactorAsync(diseaseId, pathogenId, numeric: true);
        var updated = new Range { Min = 100, IsMinExclusive = false, Max = decimal.MaxValue, IsMaxExclusive = false, Unit = "mg/L" };

        await _handler.HandleAsync(new UpdateResistanceRiskFactorCommand
        {
            Id = factorId,
            PathogenId = pathogenId,
            Name = "Elevated CRP",
            Criterion = new UpdateCriterionCommand
            {
                Name = "CRP",
                Type = CriterionType.Numeric,
                Value = updated,
            },
        }, TestContext.Current.CancellationToken);

        var saved = await GetPersistedAsync(factorId);
        Assert.Equal("Elevated CRP", saved.Name);
        var numeric = Assert.IsType<NumericCriterion>(saved.Criterion);
        Assert.Equal("CRP", numeric.Name);
        Assert.Equal(100, numeric.Value.Min);
        Assert.Equal(decimal.MaxValue, numeric.Value.Max);
        Assert.Equal("mg/L", numeric.Value.Unit);
    }

    # endregion

    # region Fail path

    [Fact]
    public async Task UpdateResistanceRiskFactor_NotFound_Fail()
    {
        var pathogenId = await SeedPathogenAsync();

        await Assert.ThrowsAsync<NotFoundException>(() => _handler.HandleAsync(
            new UpdateResistanceRiskFactorCommand
            {
                Id = Guid.CreateVersion7(),
                PathogenId = pathogenId,
                Name = "Prior antibiotic use",
                Criterion = new UpdateCriterionCommand
                {
                    Name = "Prior antibiotic use",
                    Type = CriterionType.Boolean,
                    Value = null,
                },
            }, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task UpdateResistanceRiskFactor_SoftDeleted_Fail()
    {
        // A soft-deleted factor is hidden by the query filter and must be treated as
        // not found
        var diseaseId = await SeedDiseaseAsync();
        var pathogenId = await SeedPathogenAsync();
        var factorId = await SeedFactorAsync(diseaseId, pathogenId, numeric: false, softDeletedFactor: true);

        await Assert.ThrowsAsync<NotFoundException>(() => _handler.HandleAsync(
            new UpdateResistanceRiskFactorCommand
            {
                Id = factorId,
                PathogenId = pathogenId,
                Name = "Prior antibiotic use",
                Criterion = new UpdateCriterionCommand
                {
                    Name = "Prior antibiotic use",
                    Type = CriterionType.Boolean,
                    Value = null,
                },
            }, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task UpdateResistanceRiskFactor_PathogenNotFound_Fail()
    {
        // Pathogen existence is checked BEFORE the factor is loaded
        var diseaseId = await SeedDiseaseAsync();
        var pathogenId = await SeedPathogenAsync();
        var factorId = await SeedFactorAsync(diseaseId, pathogenId, numeric: false);

        await Assert.ThrowsAsync<BadRequestException>(() => _handler.HandleAsync(
            new UpdateResistanceRiskFactorCommand
            {
                Id = factorId,
                PathogenId = Guid.CreateVersion7(),
                Name = "Prior antibiotic use",
                Criterion = new UpdateCriterionCommand
                {
                    Name = "Prior antibiotic use",
                    Type = CriterionType.Boolean,
                    Value = null,
                },
            }, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task UpdateResistanceRiskFactor_SoftDeletedPathogen_Fail()
    {
        var diseaseId = await SeedDiseaseAsync();
        var pathogenId = await SeedPathogenAsync(softDeleted: true);
        var factorId = await SeedFactorAsync(diseaseId, pathogenId, numeric: false);

        await Assert.ThrowsAsync<BadRequestException>(() => _handler.HandleAsync(
            new UpdateResistanceRiskFactorCommand
            {
                Id = factorId,
                PathogenId = pathogenId,
                Name = "Prior antibiotic use",
                Criterion = new UpdateCriterionCommand
                {
                    Name = "Prior antibiotic use",
                    Type = CriterionType.Boolean,
                    Value = null,
                },
            }, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task UpdateResistanceRiskFactor_TypeChange_FailsAtMapping()
    {
        // The criterion type is immutable. An out-of-range Type passes validation but
        // fails inside the mapper with a BadRequestException ("type mismatch").
        var diseaseId = await SeedDiseaseAsync();
        var pathogenId = await SeedPathogenAsync();
        var factorId = await SeedFactorAsync(diseaseId, pathogenId, numeric: false);

        await Assert.ThrowsAsync<BadRequestException>(() => _handler.HandleAsync(
            new UpdateResistanceRiskFactorCommand
            {
                Id = factorId,
                PathogenId = pathogenId,
                Name = "Prior antibiotic use",
                Criterion = new UpdateCriterionCommand
                {
                    Name = "Prior antibiotic use",
                    Type = (CriterionType)999,
                    Value = null,
                },
            }, TestContext.Current.CancellationToken));
    }

    # endregion
}
