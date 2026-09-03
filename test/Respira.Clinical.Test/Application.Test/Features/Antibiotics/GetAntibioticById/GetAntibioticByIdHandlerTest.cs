using Application.Contracts.Data;
using Application.Features.Antibiotics.GetAntibioticById;
using Domain.Enums;
using Domain.Models;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Respira.ServiceDefaults.Contracts.Results;
using Range = Domain.Models.Range;

namespace Application.Test.Features.Antibiotics.GetAntibioticById;

public class GetAntibioticByIdHandlerTest : IClassFixture<PostgresFixture>, IAsyncLifetime
{
    private readonly DbContextOptions<AppDbContext> _options;
    private readonly GetAntibioticByIdHandler _handler;
    private readonly IDbContext _context;

    public GetAntibioticByIdHandlerTest(PostgresFixture fixture)
    {
        // Create handler dependencies
        _options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(fixture.ConnectionString).Options;
        _context = new AppDbContext(_options);

        // Initialize handler
        _handler = new(_context);
    }

    public async ValueTask DisposeAsync()
    {
        await _context.DisposeAsync();
    }

    public async ValueTask InitializeAsync()
    {
        // Dosages, antibiotics and pathogens reference groups/each other through FKs,
        // so delete them first. IgnoreQueryFilters is needed because soft-deleted rows
        // are hidden by the query filter but still occupy the table
        await _context.Dosages.IgnoreQueryFilters()
            .ExecuteDeleteAsync(TestContext.Current.CancellationToken);
        await _context.Antibiotics.IgnoreQueryFilters()
            .ExecuteDeleteAsync(TestContext.Current.CancellationToken);
        await _context.Pathogens.IgnoreQueryFilters()
            .ExecuteDeleteAsync(TestContext.Current.CancellationToken);
        await _context.AntibioticGroups.IgnoreQueryFilters()
            .ExecuteDeleteAsync(TestContext.Current.CancellationToken);
    }

    /*
     * Seeds a two-level group hierarchy (Beta-lactams -> Penicillins) with one
     * antibiotic inside the subgroup. The antibiotic carries a standard oral dose,
     * a renal-adjusted IV dose (CrCl 30-60 ml/min) and two pathogens in its spectra
     */
    private async Task<(Antibiotic Antibiotic, AntibioticGroup Root)> SeedFullGraphAsync()
    {
        var root = new AntibioticGroup
        {
            Name = "Beta-lactams",
            Description = "Cell wall synthesis inhibitors sharing the beta-lactam ring",
            ParentId = null,
        };
        var subgroup = new AntibioticGroup
        {
            Name = "Penicillins",
            Description = "Beta-lactam antibiotics active against gram-positive organisms",
            ParentId = root.Id,
        };

        var klebsiella = new Pathogen
        {
            Name = "Klebsiella pneumoniae",
            Description = "Gram-negative bacillus causing hospital-acquired pneumonia",
        };
        var pseudomonas = new Pathogen
        {
            Name = "Pseudomonas aeruginosa",
            Description = "Gram-negative rod, common cause of ventilator-associated pneumonia",
        };

        var amoxicillin = new Antibiotic
        {
            Name = "Amoxicillin",
            AntibioticGroupId = subgroup.Id,
            Classification = AwareClassification.Access,
        };
        amoxicillin.AntibioticSpectra.Add(klebsiella);
        amoxicillin.AntibioticSpectra.Add(pseudomonas);
        amoxicillin.Dosages.Add(new Dosage
        {
            AntibioticId = amoxicillin.Id,
            RouteOfAdministration = RouteOfAdministration.Oral,
            Dose = "500 mg orally every 8 hours",
            Crcl = null,
        });
        amoxicillin.Dosages.Add(new Dosage
        {
            AntibioticId = amoxicillin.Id,
            RouteOfAdministration = RouteOfAdministration.Intravenous,
            Dose = "1 g IV every 12 hours",
            Crcl = new Range
            {
                Min = 30m,
                Max = 60m,
                IsMinExclusive = false,
                IsMaxExclusive = false,
                Unit = "ml/min"
            },
        });

        await _context.AntibioticGroups.AddRangeAsync([root, subgroup], TestContext.Current.CancellationToken);
        await _context.Pathogens.AddRangeAsync([klebsiella, pseudomonas], TestContext.Current.CancellationToken);
        await _context.Antibiotics.AddAsync(amoxicillin, TestContext.Current.CancellationToken);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);
        return (amoxicillin, root);
    }

    # region Happy path

    [Fact]
    public async Task GetAntibioticById_ReturnsFullGraph_Success()
    {
        var (seeded, root) = await SeedFullGraphAsync();

        var result = await _handler.HandleAsync(
            new GetAntibioticByIdQuery { Id = seeded.Id }, TestContext.Current.CancellationToken);
        Assert.True(result.IsSuccess);
        Assert.Null(result.Error);
        Assert.NotNull(result.Data);
        Assert.Equal(Status.Success, result.StatusCode);

        Assert.Equal(seeded.Id, result.Data.Id);
        Assert.Equal("Amoxicillin", result.Data.Name);
        Assert.Equal(AwareClassification.Access, result.Data.Classification);

        // Nested group projection with parent resolution through two levels:
        // the antibiotic sits in "Penicillins", whose parent is the root group
        Assert.Equal("Penicillins", result.Data.AntibioticGroup.Name);
        Assert.Equal("Beta-lactam antibiotics active against gram-positive organisms",
            result.Data.AntibioticGroup.Description);
        Assert.Equal(root.Id, result.Data.AntibioticGroup.ParentId);
        Assert.Equal("Beta-lactams", result.Data.AntibioticGroup.ParentName);

        // Antibiotic spectrum: both linked pathogens must appear
        Assert.Equal(2, result.Data.AntibioticSpectrum.Count);
        Assert.Contains(result.Data.AntibioticSpectrum, x => x.Name == "Klebsiella pneumoniae");
        Assert.Contains(result.Data.AntibioticSpectrum, x => x.Name == "Pseudomonas aeruginosa");

        // Dosages: one standard (no CrCl range) and one renal-adjusted
        Assert.Equal(2, result.Data.Dosages.Count);
        var standard = Assert.Single(result.Data.Dosages, x => x.RouteOfAdministration ==
            RouteOfAdministration.Oral);
        Assert.Equal("500 mg orally every 8 hours", standard.Dose);
        Assert.Null(standard.Crcl);

        var adjusted = Assert.Single(result.Data.Dosages, x => x.RouteOfAdministration ==
            RouteOfAdministration.Intravenous);
        Assert.Equal("1 g IV every 12 hours", adjusted.Dose);
        Assert.NotNull(adjusted.Crcl);
        Assert.Equal(30m, adjusted.Crcl.Min);
        Assert.Equal(60m, adjusted.Crcl.Max);
        Assert.Equal("ml/min", adjusted.Crcl.Unit);
    }

    /*=== boundary: root group has no parent name ===*/

    [Fact]
    public async Task GetAntibioticById_AntibioticInRootGroup_ParentNameIsNull_Success()
    {
        var root = new AntibioticGroup
        {
            Name = "Macrolides",
            Description = "Protein synthesis inhibitors with a macrocyclic lactone ring",
            ParentId = null,
        };
        var azithromycin = new Antibiotic
        {
            Name = "Azithromycin",
            AntibioticGroupId = root.Id,
            Classification = AwareClassification.Watch,
        };
        azithromycin.Dosages.Add(new Dosage
        {
            AntibioticId = azithromycin.Id,
            RouteOfAdministration = RouteOfAdministration.Oral,
            Dose = "250 mg orally once daily",
            Crcl = null,
        });

        await _context.AntibioticGroups.AddAsync(root, TestContext.Current.CancellationToken);
        await _context.Antibiotics.AddAsync(azithromycin, TestContext.Current.CancellationToken);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await _handler.HandleAsync(
            new GetAntibioticByIdQuery { Id = azithromycin.Id }, TestContext.Current.CancellationToken);
        Assert.True(result.IsSuccess);
        Assert.Null(result.Error);
        Assert.NotNull(result.Data);
        Assert.Equal(Status.Success, result.StatusCode);

        // Business rule: a root group has neither parent ID nor parent name
        Assert.Null(result.Data.AntibioticGroup.ParentName);
        Assert.Null(result.Data.AntibioticGroup.ParentId);

        // No spectrum linked: the list must be empty, not null
        Assert.Empty(result.Data.AntibioticSpectrum);
        var dosage = Assert.Single(result.Data.Dosages);
        Assert.Equal(RouteOfAdministration.Oral, dosage.RouteOfAdministration);
    }

    # endregion

    # region Fail path

    [Fact]
    public async Task GetAntibioticById_AntibioticNotFound_Fail()
    {
        var unknownId = Guid.CreateVersion7();

        var result = await _handler.HandleAsync(new GetAntibioticByIdQuery { Id = unknownId }, TestContext.Current.CancellationToken);
        Assert.True(result.IsFailure);
        Assert.NotNull(result.Error);
        Assert.Equal(Status.ResourceNotFound, result.StatusCode);
    }

    # endregion
}
