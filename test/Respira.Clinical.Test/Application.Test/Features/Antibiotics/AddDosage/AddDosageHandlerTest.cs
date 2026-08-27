using Application.Contracts.Data;
using Application.Features.Antibiotics.AddDosage;
using Domain.Enums;
using Domain.Models;
using Infrastructure.Data;
using Range = Domain.Models.Range;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Respira.ServiceDefaults.Exceptions;

namespace Application.Test.Features.Antibiotics.AddDosage;

public class AddDosageHandlerTest : IClassFixture<PostgresFixture>, IAsyncLifetime
{
    private readonly DbContextOptions<AppDbContext> _options;
    private readonly AddDosageHandler _handler;
    private readonly IDbContext _context;

    public AddDosageHandlerTest(PostgresFixture fixture)
    {
        // Create handler dependencies
        _options = new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(fixture.ConnectionString).Options;
        _context = new AppDbContext(_options);
        var mapper = new AddDosageMapper();
        var logger = new Mock<ILogger<AddDosageHandler>>().Object;

        // Initialize handler
        _handler = new(_context, mapper, logger);
    }

    public async ValueTask DisposeAsync()
    {
        await _context.DisposeAsync();
    }

    public async ValueTask InitializeAsync()
    {
        // Dosages reference antibiotics through an FK, so delete them first.
        // IgnoreQueryFilters is needed because soft-deleted rows are hidden by the
        // query filter but still occupy the table
        await _context.Dosages.IgnoreQueryFilters()
            .ExecuteDeleteAsync(TestContext.Current.CancellationToken);
        await _context.Antibiotics.IgnoreQueryFilters()
            .ExecuteDeleteAsync(TestContext.Current.CancellationToken);
        await _context.AntibioticGroups.IgnoreQueryFilters()
            .ExecuteDeleteAsync(TestContext.Current.CancellationToken);
    }

    /*
     * Seeds a group and an antibiotic holding initialDosages. The DosageIds primitive
     * collection is kept in sync manually since EF Core does not maintain it
     */
    private async Task<Antibiotic> SeedAsync(string antibioticName, List<(string Dose, RouteOfAdministration Route, Range? Crcl)> initialDosages, bool softDeleted = false)
    {
        var group = new AntibioticGroup
        {
            Name = "Beta-lactams",
            Description = "Cell wall synthesis inhibitors sharing the beta-lactam ring",
            ParentId = null,
        };
        var antibiotic = new Antibiotic
        {
            Name = antibioticName,
            AntibioticGroupId = group.Id,
            Classification = AwareClassification.Access,
            IsDeleted = softDeleted,
            DeletedAt = softDeleted ? DateTimeOffset.UtcNow : null,
        };
        foreach (var (dose, route, crcl) in initialDosages)
        {
            var dosage = new Dosage
            {
                AntibioticId = antibiotic.Id,
                Dose = dose,
                RouteOfAdministration = route,
                Crcl = crcl,
            };
            antibiotic.Dosages.Add(dosage);
            antibiotic.DosageIds.Add(dosage.Id);
        }

        await _context.AntibioticGroups.AddAsync(group, TestContext.Current.CancellationToken);
        await _context.Antibiotics.AddAsync(antibiotic, TestContext.Current.CancellationToken);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        return antibiotic;
    }

    private async Task<List<Dosage>> GetPersistedDosagesAsync(Guid antibioticId)
    {
        await using var freshContext = new AppDbContext(_options);
        return await freshContext.Dosages
            .Where(x => x.AntibioticId == antibioticId)
            .ToListAsync(TestContext.Current.CancellationToken);
    }

    # region Happy path

    [Fact]
    public async Task AddDosage_StandardDoseToNewRoute_Success()
    {
        // Oral standard already exists; adding the Intravenous standard dose is allowed
        var antibiotic = await SeedAsync("Amoxicillin",
        [
            ("500 mg orally every 8 hours", RouteOfAdministration.Oral, null),
        ]);

        var result = await _handler.HandleAsync(new AddDosageCommand
        {
            AntibioticId = antibiotic.Id,
            RouteOfAdministration = RouteOfAdministration.Intravenous,
            Dose = "1 g IV every 8 hours",
            Crcl = null,
        }, TestContext.Current.CancellationToken);

        Assert.NotEqual(Guid.Empty, result.Id);

        // Verify through a fresh context so the change tracker cannot mask a failed commit
        var dosages = await GetPersistedDosagesAsync(antibiotic.Id);
        Assert.Equal(2, dosages.Count);
        var added = Assert.Single(dosages, x => x.Id == result.Id);
        Assert.Equal(antibiotic.Id, added.AntibioticId);
        Assert.Equal(RouteOfAdministration.Intravenous, added.RouteOfAdministration);
        Assert.Equal("1 g IV every 8 hours", added.Dose);
        Assert.Null(added.Crcl);

        // The new dosage ID must be linked back into the antibiotic's primitive collection
        await using var linkContext = new AppDbContext(_options);
        var savedAntibiotic = await linkContext.Antibiotics.SingleAsync(
            x => x.Id == antibiotic.Id, TestContext.Current.CancellationToken);
        Assert.Contains(result.Id, savedAntibiotic.DosageIds);
    }

    [Fact]
    public async Task AddDosage_AdjustedDoseToRouteWithStandard_Success()
    {
        // Renal dose adjustment: existing oral standard plus an oral dose for reduced CrCl
        var antibiotic = await SeedAsync("Ciprofloxacin",
        [
            ("500 mg orally every 12 hours", RouteOfAdministration.Oral, null),
        ]);

        var result = await _handler.HandleAsync(new AddDosageCommand
        {
            AntibioticId = antibiotic.Id,
            RouteOfAdministration = RouteOfAdministration.Oral,
            Dose = "250 mg orally every 24 hours",
            Crcl = new Range { Min = 15, IsMinExclusive = true, Max = 29, IsMaxExclusive = true, Unit = "mL/min" },
        }, TestContext.Current.CancellationToken);

        var dosages = await GetPersistedDosagesAsync(antibiotic.Id);
        Assert.Equal(2, dosages.Count);
        var added = Assert.Single(dosages, x => x.Id == result.Id);
        Assert.NotNull(added.Crcl);
        Assert.Equal(15, added.Crcl.Min);
        Assert.Equal(29, added.Crcl.Max);
        Assert.True(added.Crcl.IsMinExclusive);
        Assert.True(added.Crcl.IsMaxExclusive);
        Assert.Equal("mL/min", added.Crcl.Unit);
    }

    [Fact]
    public async Task AddDosage_CrclRangesTouchAtExclusiveBoundary_Success()
    {
        /*
         * Boundary value technique: existing range [30, 60] (inclusive) and new range
         * (60, 90] only touch at 60 without sharing it, which is not an overlap and
         * therefore must be accepted
         */
        var antibiotic = await SeedAsync("Meropenem",
        [
            ("1 g IV every 8 hours", RouteOfAdministration.Intravenous, null),
            (
                "500 mg IV every 12 hours",
                RouteOfAdministration.Intravenous,
                new Range { Min = 30, IsMinExclusive = false, Max = 60, IsMaxExclusive = false, Unit = "mL/min" }
            ),
        ]);

        var result = await _handler.HandleAsync(new AddDosageCommand
        {
            AntibioticId = antibiotic.Id,
            RouteOfAdministration = RouteOfAdministration.Intravenous,
            Dose = "500 mg IV every 24 hours",
            Crcl = new Range { Min = 60, IsMinExclusive = true, Max = 90, IsMaxExclusive = false, Unit = "mL/min" },
        }, TestContext.Current.CancellationToken);

        var dosages = await GetPersistedDosagesAsync(antibiotic.Id);
        Assert.Equal(3, dosages.Count);
        Assert.Contains(dosages, x => x.Id == result.Id);
    }

    # endregion

    # region Fail path

    [Fact]
    public async Task AddDosage_AntibioticNotFound_Fail()
    {
        var unknownId = Guid.CreateVersion7();

        await Assert.ThrowsAsync<NotFoundException>(() => _handler.HandleAsync(
            new AddDosageCommand
            {
                AntibioticId = unknownId,
                RouteOfAdministration = RouteOfAdministration.Oral,
                Dose = "500 mg orally every 8 hours",
                Crcl = null,
            }, TestContext.Current.CancellationToken));

        Assert.Equal(0, await _context.Dosages.CountAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task AddDosage_SoftDeletedAntibiotic_Fail()
    {
        // A soft-deleted antibiotic is hidden by the query filter, so it must be
        // rejected just like an unknown antibiotic
        var deletedAntibiotic = await SeedAsync("Chloramphenicol",
        [
            ("500 mg orally every 6 hours", RouteOfAdministration.Oral, null),
        ], softDeleted: true);

        await Assert.ThrowsAsync<NotFoundException>(() => _handler.HandleAsync(
            new AddDosageCommand
            {
                AntibioticId = deletedAntibiotic.Id,
                RouteOfAdministration = RouteOfAdministration.Intravenous,
                Dose = "1 g IV every 6 hours",
                Crcl = null,
            }, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task AddDosage_SecondStandardDoseForSameRoute_Fail()
    {
        /*
         * Business rule: each route of administration must have 1 and only 1 standard
         * dose (CrCl == null). Adding another oral standard violates it
         */
        var antibiotic = await SeedAsync("Amoxicillin",
        [
            ("500 mg orally every 8 hours", RouteOfAdministration.Oral, null),
        ]);

        await Assert.ThrowsAsync<BadRequestException>(() => _handler.HandleAsync(
            new AddDosageCommand
            {
                AntibioticId = antibiotic.Id,
                RouteOfAdministration = RouteOfAdministration.Oral,
                Dose = "875 mg orally every 12 hours",
                Crcl = null,
            }, TestContext.Current.CancellationToken));

        // The rejected dosage must not be persisted nor linked to the antibiotic
        var dosages = await GetPersistedDosagesAsync(antibiotic.Id);
        _ = Assert.Single(dosages);

        await using var freshContext = new AppDbContext(_options);
        var savedAntibiotic = await freshContext.Antibiotics.SingleAsync(
            x => x.Id == antibiotic.Id, TestContext.Current.CancellationToken);
        _ = Assert.Single(savedAntibiotic.DosageIds);
    }

    [Fact]
    public async Task AddDosage_AdjustedDoseForRouteWithoutStandard_Fail()
    {
        /*
         * Business rule: every route present in the dosage list needs exactly one
         * standard dose. Introducing a brand-new route through an adjusted dose only
         * leaves that route with zero standard doses
         */
        var antibiotic = await SeedAsync("Ciprofloxacin",
        [
            ("500 mg orally every 12 hours", RouteOfAdministration.Oral, null),
        ]);

        await Assert.ThrowsAsync<BadRequestException>(() => _handler.HandleAsync(
            new AddDosageCommand
            {
                AntibioticId = antibiotic.Id,
                RouteOfAdministration = RouteOfAdministration.Intravenous,
                Dose = "400 mg IV every 12 hours",
                Crcl = new Range { Min = 15, IsMinExclusive = true, Max = 29, IsMaxExclusive = false, Unit = "mL/min" },
            }, TestContext.Current.CancellationToken));

        Assert.Single(await GetPersistedDosagesAsync(antibiotic.Id));
    }

    [Fact]
    public async Task AddDosage_OverlappedCrclRange_Fail()
    {
        /*
         * Business rule: within one route, CrCl ranges must not overlap. Existing
         * [30, 60] overlaps (45, 75) in the shared 45-60 region
         */
        var antibiotic = await SeedAsync("Meropenem",
        [
            ("1 g IV every 8 hours", RouteOfAdministration.Intravenous, null),
            (
                "500 mg IV every 12 hours",
                RouteOfAdministration.Intravenous,
                new Range { Min = 30, IsMinExclusive = false, Max = 60, IsMaxExclusive = false, Unit = "mL/min" }
            ),
        ]);

        await Assert.ThrowsAsync<BadRequestException>(() => _handler.HandleAsync(
            new AddDosageCommand
            {
                AntibioticId = antibiotic.Id,
                RouteOfAdministration = RouteOfAdministration.Intravenous,
                Dose = "500 mg IV every 24 hours",
                Crcl = new Range { Min = 45, IsMinExclusive = true, Max = 75, IsMaxExclusive = false, Unit = "mL/min" },
            }, TestContext.Current.CancellationToken));

        Assert.Equal(2, (await GetPersistedDosagesAsync(antibiotic.Id)).Count);
    }

    [Fact]
    public async Task AddDosage_CrclRangesTouchAtInclusiveBoundary_Fail()
    {
        /*
         * Boundary value technique: existing [30, 60] and new [60, 90] both claim the
         * exact point 60, which counts as an overlap and must be rejected
         */
        var antibiotic = await SeedAsync("Meropenem",
        [
            ("1 g IV every 8 hours", RouteOfAdministration.Intravenous, null),
            (
                "500 mg IV every 12 hours",
                RouteOfAdministration.Intravenous,
                new Range { Min = 30, IsMinExclusive = false, Max = 60, IsMaxExclusive = false, Unit = "mL/min" }
            ),
        ]);

        await Assert.ThrowsAsync<BadRequestException>(() => _handler.HandleAsync(
            new AddDosageCommand
            {
                AntibioticId = antibiotic.Id,
                RouteOfAdministration = RouteOfAdministration.Intravenous,
                Dose = "500 mg IV every 24 hours",
                Crcl = new Range { Min = 60, IsMinExclusive = false, Max = 90, IsMaxExclusive = false, Unit = "mL/min" },
            }, TestContext.Current.CancellationToken));

        Assert.Equal(2, (await GetPersistedDosagesAsync(antibiotic.Id)).Count);
    }

    # endregion
}
