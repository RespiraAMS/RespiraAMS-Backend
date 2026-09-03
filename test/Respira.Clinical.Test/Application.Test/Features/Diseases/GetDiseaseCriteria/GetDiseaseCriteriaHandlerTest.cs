using Application.Features.Diseases.GetDiseaseCriteria;
using Application.Features.Shared.ManageCriterion;
using Domain.Enums;
using Domain.Models;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Range = Domain.Models.Range;
using Respira.ServiceDefaults.Contracts.Results;

namespace Application.Test.Features.Diseases.GetDiseaseCriteria;

public class GetDiseaseCriteriaHandlerTest : IClassFixture<PostgresFixture>, IAsyncLifetime
{
    private readonly DbContextOptions<AppDbContext> _options;
    private readonly GetDiseaseCriteriaHandler _handler;
    private readonly AppDbContext _context;

    public GetDiseaseCriteriaHandlerTest(PostgresFixture fixture)
    {
        _options = new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(fixture.ConnectionString).Options;
        _context = new AppDbContext(_options);
        var mapper = new CriterionResultMapper();
        var logger = new Mock<ILogger<GetDiseaseCriteriaHandler>>().Object;

        _handler = new(_context, mapper, logger);
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
        _context.Diseases.RemoveRange(
            await _context.Diseases.IgnoreQueryFilters().ToListAsync(TestContext.Current.CancellationToken));
        _context.Pathogens.RemoveRange(
            await _context.Pathogens.IgnoreQueryFilters().ToListAsync(TestContext.Current.CancellationToken));
        _context.Criteria.RemoveRange(
            await _context.Criteria.IgnoreQueryFilters().ToListAsync(TestContext.Current.CancellationToken));
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    private async Task<BooleanCriterion> SeedBooleanCriterionAsync(string name)
    {
        var criterion = new BooleanCriterion { Name = name };
        _context.Criteria.Add(criterion);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);
        return criterion;
    }

    private async Task<NumericCriterion> SeedNumericCriterionAsync(string name, decimal min, decimal max)
    {
        var criterion = new NumericCriterion
        {
            Name = name,
            Value = new Range { Min = min, IsMinExclusive = false, Max = max, IsMaxExclusive = false, Unit = "breaths/min" },
        };
        _context.Criteria.Add(criterion);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);
        return criterion;
    }

    private async Task<Pathogen> SeedPathogenAsync(string name)
    {
        var pathogen = new Pathogen
        {
            Name = name,
            Description = $"Description for {name}",
        };
        _context.Pathogens.Add(pathogen);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);
        return pathogen;
    }

    private async Task<Disease> SeedDiseaseAsync(string name = "Community-Acquired Pneumonia")
    {
        var disease = new Disease
        {
            Name = name,
            Description = "Infection of the lung parenchyma acquired outside of a healthcare setting",
            IcuScoreThreshold = 3,
        };
        _context.Diseases.Add(disease);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);
        return disease;
    }

    private async Task SeedIcuCriterionAsync(Guid diseaseId, Criterion criterion, int score)
    {
        _context.IcuHospitalizeCriteria.Add(new IcuHospitalizeCriterion
        {
            DiseaseId = diseaseId,
            CriterionId = criterion.Id,
            Criterion = criterion,
            Score = score,
        });
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    private async Task SeedRiskFactorAsync(Guid diseaseId, Pathogen pathogen, Criterion criterion, string factorName)
    {
        _context.ResistanceRiskFactors.Add(new ResistanceRiskFactor
        {
            DiseaseId = diseaseId,
            PathogenId = pathogen.Id,
            Pathogen = pathogen,
            CriterionId = criterion.Id,
            Criterion = criterion,
            Name = factorName,
        });
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    private async Task SeedProtocolAsync(Guid diseaseId, List<Criterion> otherCriteria)
    {
        _context.EmpiricTreatmentProtocols.Add(new EmpiricTreatmentProtocol
        {
            Name = "IDSA/ATS 2024 CAP Empiric Guidance",
            Issuer = "Infectious Diseases Society of America",
            IssueDate = new DateOnly(2024, 8, 1),
            Version = 3,
            DiseaseId = diseaseId,
            Severity = Severity.Moderate,
            TreatmentSite = TreatmentSite.Inpatient,
            OtherCriteria = otherCriteria,
        });
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    # region Happy path

    [Fact]
    public async Task GetDiseaseCriteria_ReturnsAllThreeCategories_Success()
    {
        await CleanupAsync();
        var disease = await SeedDiseaseAsync();
        var pathogen = await SeedPathogenAsync("Streptococcus pneumoniae");
        var icuA = await SeedBooleanCriterionAsync("Prior history of COPD");
        var icuB = await SeedBooleanCriterionAsync("Age over 65");
        var risk = await SeedBooleanCriterionAsync("Prior antibiotic use within 90 days");
        var other = await SeedBooleanCriterionAsync("Immunocompromised status");

        await SeedIcuCriterionAsync(disease.Id, icuA, score: 5);
        await SeedIcuCriterionAsync(disease.Id, icuB, score: 3);
        await SeedRiskFactorAsync(disease.Id, pathogen, risk, "Beta-lactam allergy");
        await SeedProtocolAsync(disease.Id, [other]);

        var result = await _handler.HandleAsync(
            new GetDiseaseCriteriaQuery { Id = disease.Id },
            TestContext.Current.CancellationToken);
        Assert.True(result.IsSuccess);
        Assert.Null(result.Error);
        Assert.Equal(Status.Success, result.StatusCode);
        Assert.NotNull(result.Data);

        // Business rule: ICU hospitalize criteria are projected through the criterion mapper
        Assert.Equal(2, result.Data.IcuHospitalizeCriteria.Count());
        Assert.Contains(result.Data.IcuHospitalizeCriteria, x => x.Id == icuA.Id && x.Name == "Prior history of COPD");
        Assert.Contains(result.Data.IcuHospitalizeCriteria, x => x.Id == icuB.Id && x.Name == "Age over 65");
        Assert.All(result.Data.IcuHospitalizeCriteria, x =>
        {
            Assert.Equal(CriterionType.Boolean, x.Type);
            Assert.Null(x.Value);
        });

        // Business rule: resistance risk factor criteria are projected
        var riskItem = Assert.Single(result.Data.ResistanceRiskFactorCriteria);
        Assert.Equal(risk.Id, riskItem.Id);
        Assert.Equal("Prior antibiotic use within 90 days", riskItem.Name);

        // Business rule: secondary "other" criteria unique to treatment protocols are projected
        var otherItem = Assert.Single(result.Data.OtherCriteria);
        Assert.Equal(other.Id, otherItem.Id);
        Assert.Equal("Immunocompromised status", otherItem.Name);
    }

    [Fact]
    public async Task GetDiseaseCriteria_OtherCriteriaExcludesSharedCriterion_Success()
    {
        await CleanupAsync();
        var disease = await SeedDiseaseAsync();
        var shared = await SeedBooleanCriterionAsync("Prior history of COPD");

        // The same criterion is referenced both as an ICU criterion and as a protocol's
        // secondary criterion. It must appear only under ICU criteria, not under OtherCriteria.
        await SeedIcuCriterionAsync(disease.Id, shared, score: 5);
        await SeedProtocolAsync(disease.Id, [shared]);

        var result = await _handler.HandleAsync(
            new GetDiseaseCriteriaQuery { Id = disease.Id },
            TestContext.Current.CancellationToken);
        Assert.True(result.IsSuccess);
        Assert.Null(result.Error);
        Assert.Equal(Status.Success, result.StatusCode);
        Assert.NotNull(result.Data);

        // Business rule: a criterion already present in ICU/resistance categories is
        // deduplicated out of the OtherCriteria collection
        Assert.Contains(result.Data.IcuHospitalizeCriteria, x => x.Id == shared.Id);
        Assert.Empty(result.Data.OtherCriteria);
    }

    [Fact]
    public async Task GetDiseaseCriteria_OtherCriteriaDistinctAcrossProtocols_Success()
    {
        await CleanupAsync();
        var disease = await SeedDiseaseAsync();
        var other = await SeedBooleanCriterionAsync("Immunocompromised status");

        // Two protocols referencing the same secondary criterion must yield a single entry
        await SeedProtocolAsync(disease.Id, [other]);
        await SeedProtocolAsync(disease.Id, [other]);

        var result = await _handler.HandleAsync(
            new GetDiseaseCriteriaQuery { Id = disease.Id },
            TestContext.Current.CancellationToken);
        Assert.True(result.IsSuccess);
        Assert.Null(result.Error);
        Assert.Equal(Status.Success, result.StatusCode);
        Assert.NotNull(result.Data);

        // Business rule: OtherCriteria is de-duplicated by criterion Id across protocols
        var item = Assert.Single(result.Data.OtherCriteria);
        Assert.Equal(other.Id, item.Id);
    }

    [Fact]
    public async Task GetDiseaseCriteria_NumericCriterionMapped_Success()
    {
        await CleanupAsync();
        var disease = await SeedDiseaseAsync();
        // Boundary value technique: 30 breaths/min is the clinical tachypnea threshold
        var numeric = await SeedNumericCriterionAsync("Respiratory rate", min: 30, max: 100);

        await SeedIcuCriterionAsync(disease.Id, numeric, score: 4);

        var result = await _handler.HandleAsync(
            new GetDiseaseCriteriaQuery { Id = disease.Id },
            TestContext.Current.CancellationToken);
        Assert.True(result.IsSuccess);
        Assert.Null(result.Error);
        Assert.Equal(Status.Success, result.StatusCode);
        Assert.NotNull(result.Data);

        var item = Assert.Single(result.Data.IcuHospitalizeCriteria);
        Assert.Equal(numeric.Id, item.Id);
        Assert.Equal(CriterionType.Numeric, item.Type);
        Assert.NotNull(item.Value);
        Assert.Equal(30, item.Value.Min);
        Assert.Equal(100, item.Value.Max);
        Assert.Equal("breaths/min", item.Value.Unit);
    }

    [Fact]
    public async Task GetDiseaseCriteria_NoCriteria_ReturnsEmpty_Success()
    {
        await CleanupAsync();
        var disease = await SeedDiseaseAsync();

        var result = await _handler.HandleAsync(
            new GetDiseaseCriteriaQuery { Id = disease.Id },
            TestContext.Current.CancellationToken);
        Assert.True(result.IsSuccess);
        Assert.Null(result.Error);
        Assert.Equal(Status.Success, result.StatusCode);
        Assert.NotNull(result.Data);

        // Business rule: a disease without any associated criteria yields empty collections
        Assert.Empty(result.Data.IcuHospitalizeCriteria);
        Assert.Empty(result.Data.ResistanceRiskFactorCriteria);
        Assert.Empty(result.Data.OtherCriteria);
    }

    # endregion

    # region Fail path

    [Fact]
    public async Task GetDiseaseCriteria_NotFound_Fail()
    {
        await CleanupAsync();
        var unknownId = Guid.CreateVersion7();

        var result = await _handler.HandleAsync(
            new GetDiseaseCriteriaQuery { Id = unknownId },
            TestContext.Current.CancellationToken);
        Assert.True(result.IsFailure);
        Assert.NotNull(result.Error);
        Assert.Equal(Status.BadRequest, result.StatusCode);
        Assert.Null(result.Data);
    }

    [Fact]
    public async Task GetDiseaseCriteria_SoftDeleted_Fail()
    {
        await CleanupAsync();
        var disease = await SeedDiseaseAsync();
        disease.IsDeleted = true;
        disease.DeletedAt = DateTimeOffset.UtcNow;
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Business rule: a soft-deleted disease is hidden by the global query filter
        var result = await _handler.HandleAsync(
            new GetDiseaseCriteriaQuery { Id = disease.Id },
            TestContext.Current.CancellationToken);
        Assert.True(result.IsFailure);
        Assert.NotNull(result.Error);
        Assert.Equal(Status.BadRequest, result.StatusCode);
        Assert.Null(result.Data);
    }

    # endregion
}
