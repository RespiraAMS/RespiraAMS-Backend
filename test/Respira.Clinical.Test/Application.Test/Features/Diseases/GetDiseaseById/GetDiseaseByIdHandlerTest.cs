using Application.Features.Diseases.GetDiseaseById;
using Application.Features.Shared.ManageCriterion;
using Domain.Enums;
using Domain.Models;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Respira.ServiceDefaults.Exceptions;

namespace Application.Test.Features.Diseases.GetDiseaseById;

public class GetDiseaseByIdHandlerTest : IClassFixture<PostgresFixture>, IAsyncLifetime
{
    private readonly DbContextOptions<AppDbContext> _options;
    private readonly GetDiseaseByIdHandler _handler;
    private readonly AppDbContext _context;

    public GetDiseaseByIdHandlerTest(PostgresFixture fixture)
    {
        _options = new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(fixture.ConnectionString).Options;
        _context = new AppDbContext(_options);
        var mapper = new CriterionResultMapper();

        _handler = new(_context, mapper);
    }

    public async ValueTask DisposeAsync()
    {
        await _context.DisposeAsync();
    }

    public async ValueTask InitializeAsync()
    {
        await CleanupAsync();
    }

    private async Task CleanupAsync()
    {
        // Remove child entities before parents to avoid FK conflicts, bypassing the
        // soft-delete query filter so previously soft-deleted rows are also cleared.
        _context.EmpiricTreatmentProtocols.RemoveRange(
            await _context.EmpiricTreatmentProtocols.IgnoreQueryFilters().ToListAsync(TestContext.Current.CancellationToken));
        _context.ResistanceRiskFactors.RemoveRange(
            await _context.ResistanceRiskFactors.IgnoreQueryFilters().ToListAsync(TestContext.Current.CancellationToken));
        _context.IcuHospitalizeCriteria.RemoveRange(
            await _context.IcuHospitalizeCriteria.IgnoreQueryFilters().ToListAsync(TestContext.Current.CancellationToken));
        _context.Causes.RemoveRange(
            await _context.Causes.IgnoreQueryFilters().ToListAsync(TestContext.Current.CancellationToken));
        _context.Diseases.RemoveRange(
            await _context.Diseases.IgnoreQueryFilters().ToListAsync(TestContext.Current.CancellationToken));
        _context.Pathogens.RemoveRange(
            await _context.Pathogens.IgnoreQueryFilters().ToListAsync(TestContext.Current.CancellationToken));
        _context.Criteria.RemoveRange(
            await _context.Criteria.IgnoreQueryFilters().ToListAsync(TestContext.Current.CancellationToken));

        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    private async Task<(Disease disease, Pathogen pathogen, Criterion criterion)> SeedDiseaseAsync(
        bool withRelations,
        int threshold)
    {
        var pathogen = new Pathogen
        {
            Name = "Streptococcus pneumoniae",
            Description = "Gram-positive coccus and the most common bacterial cause of community-acquired pneumonia",
        };
        var criterion = new BooleanCriterion { Name = "Prior history of COPD" };

        _context.Pathogens.Add(pathogen);
        _context.Criteria.Add(criterion);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var disease = new Disease
        {
            Name = "Community-Acquired Pneumonia",
            Description = "Infection of the lung parenchyma acquired outside of a healthcare setting",
            IcuScoreThreshold = threshold,
        };
        _context.Diseases.Add(disease);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        if (withRelations)
        {
            _context.IcuHospitalizeCriteria.Add(new IcuHospitalizeCriterion
            {
                DiseaseId = disease.Id,
                CriterionId = criterion.Id,
                Criterion = criterion,
                Score = 5,
            });
            _context.ResistanceRiskFactors.Add(new ResistanceRiskFactor
            {
                DiseaseId = disease.Id,
                PathogenId = pathogen.Id,
                Pathogen = pathogen,
                CriterionId = criterion.Id,
                Criterion = criterion,
                Name = "Prior antibiotic use within 90 days",
            });
            _context.Causes.Add(new Cause
            {
                DiseaseId = disease.Id,
                PathogenId = pathogen.Id,
                Pathogen = pathogen,
                Severity = Severity.Severe,
                TreatmentSite = TreatmentSite.IntensiveCareUnit,
            });
            _context.EmpiricTreatmentProtocols.Add(new EmpiricTreatmentProtocol
            {
                Name = "IDSA/ATS 2024 CAP Empiric Guidance",
                Issuer = "Infectious Diseases Society of America",
                IssueDate = new DateOnly(2024, 8, 1),
                Version = 3,
                DiseaseId = disease.Id,
                Severity = Severity.Severe,
                TreatmentSite = TreatmentSite.IntensiveCareUnit,
            });

            await _context.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        return (disease, pathogen, criterion);
    }

    # region Happy path

    [Fact]
    public async Task GetDiseaseById_ReturnsScalarFields_EmptyRelations_Success()
    {
        await CleanupAsync();
        var (disease, _, _) = await SeedDiseaseAsync(withRelations: false, threshold: 4);

        var result = await _handler.HandleAsync(
            new GetDiseaseByIdQuery { Id = disease.Id },
            TestContext.Current.CancellationToken);

        // Business rule: scalar disease fields are projected verbatim
        Assert.Equal(disease.Id, result.Id);
        Assert.Equal("Community-Acquired Pneumonia", result.Name);
        Assert.Equal("Infection of the lung parenchyma acquired outside of a healthcare setting", result.Description);
        Assert.Equal(4, result.IcuScoreThreshold);

        // Business rule: when no relations exist, collections are returned as empty lists, never null
        Assert.Empty(result.IcuHospitalizeCriteria);
        Assert.Empty(result.ResistanceRiskFactors);
        Assert.Empty(result.Causes);
        Assert.Empty(result.EmpiricTreatmentProtocols);
    }

    // Boundary value technique: 1 is the smallest plausible ICU score threshold
    [Fact]
    public async Task GetDiseaseById_ThresholdBoundaryMin_Success()
    {
        await CleanupAsync();
        var (disease, _, _) = await SeedDiseaseAsync(withRelations: false, threshold: 1);

        var result = await _handler.HandleAsync(
            new GetDiseaseByIdQuery { Id = disease.Id },
            TestContext.Current.CancellationToken);

        Assert.Equal(1, result.IcuScoreThreshold);
        Assert.Equal(disease.Id, result.Id);
    }

    [Fact]
    public async Task GetDiseaseById_ReturnsFullGraph_Success()
    {
        await CleanupAsync();
        var (disease, _, criterion) = await SeedDiseaseAsync(withRelations: true, threshold: 5);

        var result = await _handler.HandleAsync(
            new GetDiseaseByIdQuery { Id = disease.Id },
            TestContext.Current.CancellationToken);

        // Business rule: scalar fields
        Assert.Equal(disease.Id, result.Id);
        Assert.Equal(5, result.IcuScoreThreshold);

        // Business rule: ICU hospitalize criteria are projected (ordered, with mapped criterion)
        var icu = Assert.Single(result.IcuHospitalizeCriteria);
        Assert.Equal(5, icu.Score);
        Assert.NotNull(icu.Criterion);
        Assert.Equal(criterion.Id, icu.Criterion.Id);
        Assert.Equal("Prior history of COPD", icu.Criterion.Name);
        Assert.Equal(CriterionType.Boolean, icu.Criterion.Type);
        Assert.Null(icu.Criterion.Value);

        // Business rule: resistance risk factors carry pathogen name and mapped criterion
        var rrf = Assert.Single(result.ResistanceRiskFactors);
        Assert.Equal("Prior antibiotic use within 90 days", rrf.Name);
        Assert.Equal("Streptococcus pneumoniae", rrf.PathogenName);
        Assert.NotNull(rrf.Criterion);
        Assert.Equal("Prior history of COPD", rrf.Criterion.Name);

        // Business rule: causes project pathogen, severity and treatment site
        var cause = Assert.Single(result.Causes);
        Assert.Equal("Streptococcus pneumoniae", cause.PathogenName);
        Assert.Equal(Severity.Severe, cause.Severity);
        Assert.Equal(TreatmentSite.IntensiveCareUnit, cause.TreatmentSite);

        // Business rule: empiric treatment protocols are projected with their metadata
        var protocol = Assert.Single(result.EmpiricTreatmentProtocols);
        Assert.Equal("IDSA/ATS 2024 CAP Empiric Guidance", protocol.Name);
        Assert.Equal("Infectious Diseases Society of America", protocol.Issuer);
        Assert.Equal(new DateOnly(2024, 8, 1), protocol.IssueDate);
        Assert.Equal(3, protocol.Version);
    }

    # endregion

    # region Fail path

    [Fact]
    public async Task GetDiseaseById_NotFound_Fail()
    {
        await CleanupAsync();
        var unknownId = Guid.CreateVersion7();

        // Business rule: a missing disease is reported as NotFound
        await Assert.ThrowsAsync<NotFoundException>(() => _handler.HandleAsync(
            new GetDiseaseByIdQuery { Id = unknownId },
            TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task GetDiseaseById_SoftDeletedNotReturned_Fail()
    {
        await CleanupAsync();
        var (disease, _, _) = await SeedDiseaseAsync(withRelations: false, threshold: 3);

        // Business rule: soft-deleted diseases are hidden by the global query filter
        disease.IsDeleted = true;
        disease.DeletedAt = DateTimeOffset.UtcNow;
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        await Assert.ThrowsAsync<NotFoundException>(() => _handler.HandleAsync(
            new GetDiseaseByIdQuery { Id = disease.Id },
            TestContext.Current.CancellationToken));
    }

    # endregion
}
