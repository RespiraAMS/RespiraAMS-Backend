using Application.Features.Diagnose.TargetedDiagnose;
using Domain.Enums;
using Domain.Models;
using Domain.Services.Dtos;
using Domain.Services.Implementations;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Range = Domain.Models.Range;
using AntibiogramModel = Domain.Models.Antibiogram;
using Respira.ServiceDefaults.Exceptions;

namespace Application.Test.Features.Diagnose.TargetedDiagnose;

public class TargetedDiagnoseHandlerTest : IClassFixture<PostgresFixture>, IAsyncLifetime
{
    private readonly DbContextOptions<AppDbContext> _options;
    private readonly TargetedDiagnoseHandler _handler;
    private readonly AppDbContext _context;

    public TargetedDiagnoseHandlerTest(PostgresFixture fixture)
    {
        _options = new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(fixture.ConnectionString).Options;
        _context = new AppDbContext(_options);

        // The application registers IDiagnoseService as CapDiagnoseService (the untested path)
        var service = new CapDiagnoseService(
            new Mock<ILogger<CapDiagnoseService>>().Object,
            Options.Create(new TuningCoefficient()));
        var mapper = new TargetedDiagnoseMapper();
        var logger = new Mock<ILogger<TargetedDiagnoseHandler>>().Object;

        _handler = new(_context, service, mapper, logger);
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
        _context.Antibiograms.RemoveRange(
            await _context.Antibiograms.IgnoreQueryFilters().ToListAsync(TestContext.Current.CancellationToken));
        _context.Dosages.RemoveRange(
            await _context.Dosages.IgnoreQueryFilters().ToListAsync(TestContext.Current.CancellationToken));
        _context.Antibiotics.RemoveRange(
            await _context.Antibiotics.IgnoreQueryFilters().ToListAsync(TestContext.Current.CancellationToken));
        _context.AntibioticGroups.RemoveRange(
            await _context.AntibioticGroups.IgnoreQueryFilters().ToListAsync(TestContext.Current.CancellationToken));
        _context.Pathogens.RemoveRange(
            await _context.Pathogens.IgnoreQueryFilters().ToListAsync(TestContext.Current.CancellationToken));
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    private async Task<Pathogen> SeedPathogenAsync(string name = "Streptococcus pneumoniae")
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

    private async Task<AntibioticGroup> SeedGroupAsync(string name)
    {
        var group = new AntibioticGroup
        {
            Name = name,
            Description = $"Group for {name}",
            ParentId = null,
        };
        _context.AntibioticGroups.Add(group);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);
        return group;
    }

    private async Task<Antibiotic> SeedAntibioticAsync(
        string name,
        AntibioticGroup group,
        AwareClassification classification,
        List<(RouteOfAdministration route, string dose, Range? crcl)> dosages)
    {
        var antibiotic = new Antibiotic
        {
            Name = name,
            AntibioticGroupId = group.Id,
            AntibioticGroup = group,
            Classification = classification,
        };
        _context.Antibiotics.Add(antibiotic);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        foreach (var (route, dose, crcl) in dosages)
        {
            _context.Dosages.Add(new Dosage
            {
                AntibioticId = antibiotic.Id,
                RouteOfAdministration = route,
                Dose = dose,
                Crcl = crcl,
            });
        }

        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);
        return antibiotic;
    }

    private async Task<AntibiogramModel> SeedAntibiogramAsync(
        Guid pathogenId,
        List<Antibiotic> firstPriority,
        List<Antibiotic> secondPriority)
    {
        var antibiogram = new AntibiogramModel
        {
            PathogenId = pathogenId,
            MicLevel = MinimumInhibitoryConcentration.Susceptible,
            FirstPriorityMedicines = firstPriority,
            SecondPriorityMedicines = secondPriority,
        };
        _context.Antibiograms.Add(antibiogram);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);
        return antibiogram;
    }

    private static TargetedDiagnoseQuery QueryFor(Guid pathogenId, int ageYears, decimal weight, decimal height, decimal scr, bool isMale = true)
    {
        return new TargetedDiagnoseQuery
        {
            PathogenId = pathogenId,
            DateOfBirth = DateOnly.FromDateTime(DateTime.UtcNow).AddYears(-ageYears),
            Weight = weight,
            Height = height,
            SerumCreatine = scr,
            IsMale = isMale,
        };
    }

    # region Happy path

    [Fact]
    public async Task TargetedDiagnose_ReturnsRecommendationsAndMedicines_Success()
    {
        await CleanupAsync();
        var pathogen = await SeedPathogenAsync();
        var betaLactam = await SeedGroupAsync("Beta-lactams");
        var macrolide = await SeedGroupAsync("Macrolides");
        var fluoro = await SeedGroupAsync("Fluoroquinolones");

        var amox = await SeedAntibioticAsync("Amoxicillin", betaLactam, AwareClassification.Access,
            [(RouteOfAdministration.Oral, "500 mg every 8 hours", null)]);
        var azith = await SeedAntibioticAsync("Azithromycin", macrolide, AwareClassification.Watch,
            [(RouteOfAdministration.Oral, "500 mg once daily", null)]);
        var cipro = await SeedAntibioticAsync("Ciprofloxacin", fluoro, AwareClassification.Watch,
            [(RouteOfAdministration.Oral, "500 mg every 12 hours", null)]);

        await SeedAntibiogramAsync(pathogen.Id, [amox, azith], [cipro]);

        // 50yo male, 70kg, 1.7m, scr 1.0 -> Cockcroft-Gault CrCl = (140-50)*70/(72*1) = 87.5
        var result = await _handler.HandleAsync(
            QueryFor(pathogen.Id, ageYears: 50, weight: 70, height: 1.7m, scr: 1.0m),
            TestContext.Current.CancellationToken);

        // Business rule: CrCl is computed (Cockcroft-Gault) and positive
        Assert.Equal(87.5m, result.Crcl);

        // Business rule: recommended medicines are one-per-group, taken from the first priority list
        Assert.Equal(2, result.Medicines.Count);
        Assert.Contains(result.Medicines, m => m.Id == amox.Id && m.Name == "Amoxicillin");
        Assert.Contains(result.Medicines, m => m.Id == azith.Id && m.Name == "Azithromycin");
        Assert.All(result.Medicines, m =>
        {
            Assert.Equal(m.AntibioticGroupId, m.AntibioticGroupId);
            Assert.False(string.IsNullOrEmpty(m.AntibioticGroupName));
            Assert.NotEmpty(m.Dosages);
        });

        // Business rule: recommendations include both first and second priority medicines
        Assert.Equal(3, result.Recommendations.Count);
        Assert.Contains(result.Recommendations, m => m.Id == amox.Id);
        Assert.Contains(result.Recommendations, m => m.Id == azith.Id);
        Assert.Contains(result.Recommendations, m => m.Id == cipro.Id);
    }

    [Fact]
    public async Task TargetedDiagnose_AdjustsDosageForLowCrCl_Success()
    {
        await CleanupAsync();
        var pathogen = await SeedPathogenAsync();
        var group = await SeedGroupAsync("Beta-lactams");
        var amox = await SeedAntibioticAsync("Amoxicillin", group, AwareClassification.Access,
        [
            (RouteOfAdministration.Oral, "500 mg every 8 hours", null), // standard
            (RouteOfAdministration.Oral, "250 mg every 12 hours", new Range { Min = 10, IsMinExclusive = false, Max = 50, IsMaxExclusive = false, Unit = null }), // CrCl 10-50
        ]);
        var secondGroup = await SeedGroupAsync("Aminoglycosides");
        var gentamicin = await SeedAntibioticAsync("Gentamicin", secondGroup, AwareClassification.Watch,
            [(RouteOfAdministration.Intravenous, "5 mg/kg every 24 hours", null)]);
        await SeedAntibiogramAsync(pathogen.Id, [amox], [gentamicin]);

        // 70yo male, 60kg, 1.7m, scr 3.0 -> CrCl = (140-70)*60/(72*3) ~ 19.4 (within 10-50)
        var result = await _handler.HandleAsync(
            QueryFor(pathogen.Id, ageYears: 70, weight: 60, height: 1.7m, scr: 3.0m),
            TestContext.Current.CancellationToken);

        var medicine = Assert.Single(result.Medicines);
        // Business rule: when CrCl falls in the adjusted range, the renal-adjusted dose is used
        var dosage = Assert.Single(medicine.Dosages);
        Assert.Equal("250 mg every 12 hours", dosage.Dose);
    }

    [Fact]
    public async Task TargetedDiagnose_UsesStandardDosageForHighCrCl_Success()
    {
        await CleanupAsync();
        var pathogen = await SeedPathogenAsync();
        var group = await SeedGroupAsync("Beta-lactams");
        var amox = await SeedAntibioticAsync("Amoxicillin", group, AwareClassification.Access,
        [
            (RouteOfAdministration.Oral, "500 mg every 8 hours", null), // standard
            (RouteOfAdministration.Oral, "250 mg every 12 hours", new Range { Min = 10, IsMinExclusive = false, Max = 50, IsMaxExclusive = false, Unit = null }),
        ]);
        var secondGroup = await SeedGroupAsync("Aminoglycosides");
        var gentamicin = await SeedAntibioticAsync("Gentamicin", secondGroup, AwareClassification.Watch,
            [(RouteOfAdministration.Intravenous, "5 mg/kg every 24 hours", null)]);
        await SeedAntibiogramAsync(pathogen.Id, [amox], [gentamicin]);

        // 50yo male, 70kg, 1.7m, scr 1.0 -> CrCl = 87.5 (above the 10-50 adjusted range)
        var result = await _handler.HandleAsync(
            QueryFor(pathogen.Id, ageYears: 50, weight: 70, height: 1.7m, scr: 1.0m),
            TestContext.Current.CancellationToken);

        var medicine = Assert.Single(result.Medicines);
        // Business rule: when CrCl is above the adjusted range, the standard dose is used
        var dosage = Assert.Single(medicine.Dosages);
        Assert.Equal("500 mg every 8 hours", dosage.Dose);
    }

    [Fact]
    public async Task TargetedDiagnose_RecommendsLowestAwareClassificationPerGroup_Success()
    {
        await CleanupAsync();
        var pathogen = await SeedPathogenAsync();
        var group = await SeedGroupAsync("Beta-lactams");
        var amox = await SeedAntibioticAsync("Amoxicillin", group, AwareClassification.Access,
            [(RouteOfAdministration.Oral, "500 mg every 8 hours", null)]);
        var meropenem = await SeedAntibioticAsync("Meropenem", group, AwareClassification.Reserve,
            [(RouteOfAdministration.Intravenous, "1 g every 8 hours", null)]);

        // Both in the same group, both in the first priority list
        var secondGroup = await SeedGroupAsync("Aminoglycosides");
        var gentamicin = await SeedAntibioticAsync("Gentamicin", secondGroup, AwareClassification.Watch,
            [(RouteOfAdministration.Intravenous, "5 mg/kg every 24 hours", null)]);
        await SeedAntibiogramAsync(pathogen.Id, [amox, meropenem], [gentamicin]);

        var result = await _handler.HandleAsync(
            QueryFor(pathogen.Id, ageYears: 50, weight: 70, height: 1.7m, scr: 1.0m),
            TestContext.Current.CancellationToken);

        // Business rule: only one medicine per antibiotic group is recommended, choosing the
        // lowest (best) AWaRe classification
        var medicine = Assert.Single(result.Medicines);
        Assert.Equal(amox.Id, medicine.Id);
        Assert.Equal(AwareClassification.Access, medicine.Classification);
    }

    # endregion

    # region Fail path

    [Fact]
    public async Task TargetedDiagnose_PathogenNotFound_Fail()
    {
        await CleanupAsync();
        var unknownId = Guid.CreateVersion7();

        await Assert.ThrowsAsync<NotFoundException>(() => _handler.HandleAsync(
            QueryFor(unknownId, ageYears: 50, weight: 70, height: 1.7m, scr: 1.0m),
            TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task TargetedDiagnose_AntibiogramNotFound_Fail()
    {
        await CleanupAsync();
        var pathogen = await SeedPathogenAsync();
        // Pathogen exists but no antibiogram is associated with it

        await Assert.ThrowsAsync<UnexpectedException>(() => _handler.HandleAsync(
            QueryFor(pathogen.Id, ageYears: 50, weight: 70, height: 1.7m, scr: 1.0m),
            TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task TargetedDiagnose_EmptyFirstPriority_ThrowsBadRequest_Fail()
    {
        await CleanupAsync();
        var pathogen = await SeedPathogenAsync();
        // Antibiogram with no first-priority medicines: GetRecommendedMedicines throws
        await SeedAntibiogramAsync(pathogen.Id, [], []);

        await Assert.ThrowsAsync<BadRequestException>(() => _handler.HandleAsync(
            QueryFor(pathogen.Id, ageYears: 50, weight: 70, height: 1.7m, scr: 1.0m),
            TestContext.Current.CancellationToken));
    }

    # endregion
}
