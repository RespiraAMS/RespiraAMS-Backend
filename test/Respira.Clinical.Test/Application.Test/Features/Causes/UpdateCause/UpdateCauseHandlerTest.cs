using Application.Contracts.Data;
using Application.Features.Causes.UpdateCause;
using Domain.Enums;
using Domain.Models;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Respira.ServiceDefaults.Contracts.Results;

namespace Application.Test.Features.Causes.UpdateCause;

public class UpdateCauseHandlerTest : IClassFixture<PostgresFixture>, IAsyncLifetime
{
    private readonly DbContextOptions<AppDbContext> _options;
    private readonly UpdateCauseHandler _handler;
    private readonly IDbContext _context;

    public UpdateCauseHandlerTest(PostgresFixture fixture)
    {
        // Create handler dependencies
        _options = new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(fixture.ConnectionString).Options;
        _context = new AppDbContext(_options);
        var mapper = new UpdateCauseMapper();
        var logger = new Mock<ILogger<UpdateCauseHandler>>().Object;

        // Initialize handler
        _handler = new(_context, mapper, logger);
    }

    public async ValueTask DisposeAsync()
    {
        await _context.DisposeAsync();
    }

    public async ValueTask InitializeAsync()
    {
        // Causes reference diseases and pathogens through FKs, so delete them first.
        // IgnoreQueryFilters is needed because soft-deleted rows are hidden by the
        // query filter but still occupy the table
        await _context.Causes.IgnoreQueryFilters()
            .ExecuteDeleteAsync(TestContext.Current.CancellationToken);
        await _context.Diseases.IgnoreQueryFilters()
            .ExecuteDeleteAsync(TestContext.Current.CancellationToken);
        await _context.Pathogens.IgnoreQueryFilters()
            .ExecuteDeleteAsync(TestContext.Current.CancellationToken);
    }

    /*
     * Seeds the first disease+pathogen pair (community-acquired pneumonia with a
     * realistic CURB-65 based ICU threshold, Klebsiella) plus the given causes for
     * that pair. When secondPairCauses is provided, a second disease+pathogen pair
     * (hospital-acquired pneumonia, Pseudomonas) is seeded with those causes.
     * Flags allow seeding a soft-deleted first cause to exercise the query filter
     */
    private async Task<List<Cause>> SeedAsync(
        List<(Severity Severity, TreatmentSite TreatmentSite)> causes,
        bool softDeletedFirst = false,
        List<(Severity Severity, TreatmentSite TreatmentSite)>? secondPairCauses = null)
    {
        var disease = new Disease
        {
            Name = "Community-acquired pneumonia",
            Description = "Acute lung infection acquired outside of healthcare settings",
            IcuScoreThreshold = 3,
        };
        var pathogen = new Pathogen
        {
            Name = "Klebsiella pneumoniae",
            Description = "Gram-negative bacillus",
        };
        await _context.Diseases.AddAsync(disease, TestContext.Current.CancellationToken);
        await _context.Pathogens.AddAsync(pathogen, TestContext.Current.CancellationToken);

        var seeded = new List<Cause>();
        for (var i = 0; i < causes.Count; i++)
        {
            var cause = new Cause
            {
                DiseaseId = disease.Id,
                PathogenId = pathogen.Id,
                Severity = causes[i].Severity,
                TreatmentSite = causes[i].TreatmentSite,
                IsDeleted = i == 0 && softDeletedFirst,
                DeletedAt = i == 0 && softDeletedFirst ? DateTimeOffset.UtcNow : null,
            };
            seeded.Add(cause);
            await _context.Causes.AddAsync(cause, TestContext.Current.CancellationToken);
        }

        if (secondPairCauses is not null)
        {
            var secondDisease = new Disease
            {
                Name = "Hospital-acquired pneumonia",
                Description = "Lung infection acquired 48+ hours after hospital admission",
                IcuScoreThreshold = 2,
            };
            var secondPathogen = new Pathogen
            {
                Name = "Pseudomonas aeruginosa",
                Description = "Gram-negative rod",
            };
            await _context.Diseases.AddAsync(secondDisease, TestContext.Current.CancellationToken);
            await _context.Pathogens.AddAsync(secondPathogen, TestContext.Current.CancellationToken);

            foreach (var (severity, treatmentSite) in secondPairCauses)
            {
                var cause = new Cause
                {
                    DiseaseId = secondDisease.Id,
                    PathogenId = secondPathogen.Id,
                    Severity = severity,
                    TreatmentSite = treatmentSite,
                };
                seeded.Add(cause);
                await _context.Causes.AddAsync(cause, TestContext.Current.CancellationToken);
            }
        }

        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);
        return seeded;
    }

    # region Happy path

    [Fact]
    public async Task UpdateCause_ChangeSeverityAndTreatmentSite_Success()
    {
        var causes = await SeedAsync(
        [
            (Severity.Mild, TreatmentSite.Outpatient),
            (Severity.Severe, TreatmentSite.IntensiveCareUnit),
        ]);
        var target = causes[0];
        var updatedAtBefore = target.UpdatedAt;

        var result = await _handler.HandleAsync(new UpdateCauseCommand
        {
            Id = target.Id,
            Severity = Severity.Moderate,
            TreatmentSite = TreatmentSite.Inpatient,
        }, TestContext.Current.CancellationToken);
        Assert.True(result.IsSuccess);
        Assert.Null(result.Error);
        Assert.Equal(Status.Updated, result.StatusCode);

        // Verify through a fresh context so the change tracker cannot mask a failed commit
        await using var freshContext = new AppDbContext(_options);
        var saved = await freshContext.Causes
            .SingleAsync(x => x.Id == target.Id, TestContext.Current.CancellationToken);
        Assert.Equal(Severity.Moderate, saved.Severity);
        Assert.Equal(TreatmentSite.Inpatient, saved.TreatmentSite);
        Assert.True(saved.UpdatedAt > updatedAtBefore);

        // The sibling cause must stay untouched
        var sibling = await freshContext.Causes
            .SingleAsync(x => x.Id == causes[1].Id, TestContext.Current.CancellationToken);
        Assert.Equal(Severity.Severe, sibling.Severity);
        Assert.Equal(TreatmentSite.IntensiveCareUnit, sibling.TreatmentSite);
    }

    [Fact]
    public async Task UpdateCause_ToTupleHeldByOtherDiseasePathogenPair_Success()
    {
        /*
         * The duplicate business rule is scoped to the disease+pathogen pair: causes
         * of different pairs may freely share the same (severity, treatment site)
         * values
         */
        var causes = await SeedAsync(
        [
            (Severity.Mild, TreatmentSite.Outpatient),
        ], secondPairCauses:
        [
            (Severity.Mild, TreatmentSite.Outpatient),
        ]);

        // Update the second pair's cause onto values equal to the first pair's
        // cause - allowed because they belong to different disease+pathogen pairs
        var result = await _handler.HandleAsync(new UpdateCauseCommand
        {
            Id = causes[1].Id,
            Severity = Severity.Moderate,
            TreatmentSite = TreatmentSite.Inpatient,
        }, TestContext.Current.CancellationToken);
        Assert.True(result.IsSuccess);
        Assert.Null(result.Error);
        Assert.Equal(Status.Updated, result.StatusCode);

        await using var freshContext = new AppDbContext(_options);
        var saved = await freshContext.Causes
            .SingleAsync(x => x.Id == causes[1].Id, TestContext.Current.CancellationToken);
        Assert.Equal(Severity.Moderate, saved.Severity);
        Assert.Equal(TreatmentSite.Inpatient, saved.TreatmentSite);
    }

    [Fact]
    public async Task UpdateCause_ToValuesOfSoftDeletedCause_Success()
    {
        // A soft-deleted cause is hidden by the query filter, so taking over its
        // (severity, treatment site) values is not treated as a duplicate
        var causes = await SeedAsync(
        [
            (Severity.Mild, TreatmentSite.Outpatient),
            (Severity.Severe, TreatmentSite.IntensiveCareUnit),
        ], softDeletedFirst: false);

        // Manually soft-delete the sibling holding (Severe, ICU)
        var sibling = causes[1];
        sibling.IsDeleted = true;
        sibling.DeletedAt = DateTimeOffset.UtcNow;
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await _handler.HandleAsync(new UpdateCauseCommand
        {
            Id = causes[0].Id,
            Severity = Severity.Severe,
            TreatmentSite = TreatmentSite.IntensiveCareUnit,
        }, TestContext.Current.CancellationToken);
        Assert.True(result.IsSuccess);
        Assert.Null(result.Error);
        Assert.Equal(Status.Updated, result.StatusCode);

        await using var freshContext = new AppDbContext(_options);
        var saved = await freshContext.Causes
            .SingleAsync(x => x.Id == causes[0].Id, TestContext.Current.CancellationToken);
        Assert.Equal(Severity.Severe, saved.Severity);
        Assert.Equal(TreatmentSite.IntensiveCareUnit, saved.TreatmentSite);
    }

    [Fact]
    public async Task UpdateCause_KeepSameValues_Success()
    {
        var causes = await SeedAsync(
        [
            (Severity.Mild, TreatmentSite.Outpatient),
        ]);

        var result = await _handler.HandleAsync(new UpdateCauseCommand
        {
            Id = causes[0].Id,
            Severity = Severity.Mild,
            TreatmentSite = TreatmentSite.Outpatient,
        }, TestContext.Current.CancellationToken);
        Assert.True(result.IsSuccess);
        Assert.Null(result.Error);
        Assert.Equal(Status.Updated, result.StatusCode);

        // The no-op update must be accepted and leave the stored values intact
        await using var freshContext = new AppDbContext(_options);
        var saved = await freshContext.Causes
            .SingleAsync(x => x.Id == causes[0].Id, TestContext.Current.CancellationToken);
        Assert.Equal(Severity.Mild, saved.Severity);
        Assert.Equal(TreatmentSite.Outpatient, saved.TreatmentSite);
    }

    # endregion

    # region Fail path

    [Fact]
    public async Task UpdateCause_NotFound_Fail()
    {
        var unknownId = Guid.CreateVersion7();

        var result = await _handler.HandleAsync(
            new UpdateCauseCommand
            {
                Id = unknownId,
                Severity = Severity.Moderate,
                TreatmentSite = TreatmentSite.Inpatient,
            }, TestContext.Current.CancellationToken);
        Assert.True(result.IsFailure);
        Assert.NotNull(result.Error);
        Assert.Equal(Status.BadRequest, result.StatusCode);

        // Nothing must exist when the target was never there
        Assert.Equal(0, await _context.Causes.IgnoreQueryFilters()
            .CountAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task UpdateCause_SoftDeletedCause_Fail()
    {
        // A soft-deleted cause is hidden by the query filter, so it must be
        // rejected just like an unknown cause
        var causes = await SeedAsync(
        [
            (Severity.Mild, TreatmentSite.Outpatient),
        ], softDeletedFirst: true);

        var result = await _handler.HandleAsync(
            new UpdateCauseCommand
            {
                Id = causes[0].Id,
                Severity = Severity.Moderate,
                TreatmentSite = TreatmentSite.Inpatient,
            }, TestContext.Current.CancellationToken);
        Assert.True(result.IsFailure);
        Assert.NotNull(result.Error);
        Assert.Equal(Status.BadRequest, result.StatusCode);
    }

    [Fact]
    public async Task UpdateCause_ToDuplicateTuple_Fail()
    {
        /*
         * Business rule: updating a cause onto the (severity, treatment site)
         * values already held by a sibling of the same disease+pathogen pair
         * must be rejected
         */
        var causes = await SeedAsync(
        [
            (Severity.Mild, TreatmentSite.Outpatient),
            (Severity.Severe, TreatmentSite.IntensiveCareUnit),
        ]);

        var result = await _handler.HandleAsync(
            new UpdateCauseCommand
            {
                Id = causes[0].Id,
                Severity = Severity.Severe,
                TreatmentSite = TreatmentSite.IntensiveCareUnit,
            }, TestContext.Current.CancellationToken);
        Assert.True(result.IsFailure);
        Assert.NotNull(result.Error);
        Assert.Equal(Status.BadRequest, result.StatusCode);

        // The rejected update must leave both causes unchanged
        await using var freshContext = new AppDbContext(_options);
        var first = await freshContext.Causes
            .SingleAsync(x => x.Id == causes[0].Id, TestContext.Current.CancellationToken);
        Assert.Equal(Severity.Mild, first.Severity);
        Assert.Equal(TreatmentSite.Outpatient, first.TreatmentSite);
    }

    # endregion
}
