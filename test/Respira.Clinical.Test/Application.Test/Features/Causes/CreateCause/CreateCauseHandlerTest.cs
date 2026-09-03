using Application.Contracts.Data;
using Application.Features.Causes.CreateCause;
using Domain.Enums;
using Domain.Models;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Respira.ServiceDefaults.Contracts.Results;

namespace Application.Test.Features.Causes.CreateCause;

public class CreateCauseHandlerTest : IClassFixture<PostgresFixture>, IAsyncLifetime
{
    private readonly DbContextOptions<AppDbContext> _options;
    private readonly CreateCauseHandler _handler;
    private readonly IDbContext _context;

    public CreateCauseHandlerTest(PostgresFixture fixture)
    {
        // Create handler dependencies
        _options = new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(fixture.ConnectionString).Options;
        _context = new AppDbContext(_options);
        var mapper = new CreateCauseMapper();
        var logger = new Mock<ILogger<CreateCauseHandler>>().Object;

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
     * Seeds one disease (community-acquired pneumonia with a realistic CURB-65 based
     * ICU threshold) and one pathogen. Flags allow soft-deleting either of them to
     * exercise the query filters
     */
    private async Task<(Guid DiseaseId, Guid PathogenId)> SeedAsync(
        bool softDeletedDisease = false, bool softDeletedPathogen = false)
    {
        var disease = new Disease
        {
            Name = "Community-acquired pneumonia",
            Description = "Acute lung infection acquired outside of healthcare settings",
            IcuScoreThreshold = 3,
            IsDeleted = softDeletedDisease,
            DeletedAt = softDeletedDisease ? DateTimeOffset.UtcNow : null,
        };
        var pathogen = new Pathogen
        {
            Name = "Klebsiella pneumoniae",
            Description = "Gram-negative bacillus",
            IsDeleted = softDeletedPathogen,
            DeletedAt = softDeletedPathogen ? DateTimeOffset.UtcNow : null,
        };

        await _context.Diseases.AddAsync(disease, TestContext.Current.CancellationToken);
        await _context.Pathogens.AddAsync(pathogen, TestContext.Current.CancellationToken);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        return (disease.Id, pathogen.Id);
    }

    # region Happy path

    [Fact]
    public async Task CreateCause_Success()
    {
        var (diseaseId, pathogenId) = await SeedAsync();

        var result = await _handler.HandleAsync(new CreateCauseCommand
        {
            DiseaseId = diseaseId,
            PathogenId = pathogenId,
            Severity = Severity.Moderate,
            TreatmentSite = TreatmentSite.Inpatient,
        }, TestContext.Current.CancellationToken);
        Assert.True(result.IsSuccess);
        Assert.Null(result.Error);
        Assert.Equal(Status.Created, result.StatusCode);
        Assert.NotNull(result.Data);

        Assert.NotEqual(Guid.Empty, result.Data.Id);

        // Verify through a fresh context so the change tracker cannot mask a failed commit
        await using var freshContext = new AppDbContext(_options);
        var saved = await freshContext.Causes
            .SingleAsync(x => x.Id == result.Data.Id, TestContext.Current.CancellationToken);
        Assert.Equal(diseaseId, saved.DiseaseId);
        Assert.Equal(pathogenId, saved.PathogenId);
        Assert.Equal(Severity.Moderate, saved.Severity);
        Assert.Equal(TreatmentSite.Inpatient, saved.TreatmentSite);
    }

    [Fact]
    public async Task CreateCause_SameDiseaseAndPathogenDifferentAttributes_Success()
    {
        /*
         * The uniqueness business rule covers the whole tuple
         * (disease, pathogen, severity, treatment site): the same pair can carry
         * several cause rows as long as severity or treatment site differs
         */
        var (diseaseId, pathogenId) = await SeedAsync();
        await _context.Causes.AddAsync(new Cause
        {
            DiseaseId = diseaseId,
            PathogenId = pathogenId,
            Severity = Severity.Mild,
            TreatmentSite = TreatmentSite.Outpatient,
        }, TestContext.Current.CancellationToken);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await _handler.HandleAsync(new CreateCauseCommand
        {
            DiseaseId = diseaseId,
            PathogenId = pathogenId,
            Severity = Severity.Severe,
            TreatmentSite = TreatmentSite.IntensiveCareUnit,
        }, TestContext.Current.CancellationToken);
        Assert.True(result.IsSuccess);
        Assert.Null(result.Error);
        Assert.Equal(Status.Created, result.StatusCode);
        Assert.NotNull(result.Data);

        Assert.NotEqual(Guid.Empty, result.Data.Id);

        await using var freshContext = new AppDbContext(_options);
        Assert.Equal(2, await freshContext.Causes
            .CountAsync(x => x.DiseaseId == diseaseId && x.PathogenId == pathogenId,
                TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task CreateCause_SoftDeletedCauseCanBeRecreated_Success()
    {
        /*
         * Uniqueness is enforced at the application level (per the handler docs,
         * soft delete replaces a DB UNIQUE index), so a soft-deleted cause is hidden
         * by the query filter and the same tuple can be created again
         */
        var (diseaseId, pathogenId) = await SeedAsync();
        var original = new Cause
        {
            DiseaseId = diseaseId,
            PathogenId = pathogenId,
            Severity = Severity.Mild,
            TreatmentSite = TreatmentSite.Outpatient,
            IsDeleted = true,
            DeletedAt = DateTimeOffset.UtcNow,
        };
        await _context.Causes.AddAsync(original, TestContext.Current.CancellationToken);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await _handler.HandleAsync(new CreateCauseCommand
        {
            DiseaseId = diseaseId,
            PathogenId = pathogenId,
            Severity = Severity.Mild,
            TreatmentSite = TreatmentSite.Outpatient,
        }, TestContext.Current.CancellationToken);
        Assert.True(result.IsSuccess);
        Assert.Null(result.Error);
        Assert.Equal(Status.Created, result.StatusCode);
        Assert.NotNull(result.Data);

        Assert.NotEqual(Guid.Empty, result.Data.Id);
        Assert.NotEqual(original.Id, result.Data.Id);
        Assert.Equal(2, await _context.Causes.IgnoreQueryFilters()
            .CountAsync(TestContext.Current.CancellationToken));
    }

    # endregion

    # region Fail path

    [Fact]
    public async Task CreateCause_DiseaseNotFound_Fail()
    {
        var (_, pathogenId) = await SeedAsync();
        var unknownDiseaseId = Guid.CreateVersion7();

        var result = await _handler.HandleAsync(
            new CreateCauseCommand
            {
                DiseaseId = unknownDiseaseId,
                PathogenId = pathogenId,
                Severity = Severity.Moderate,
                TreatmentSite = TreatmentSite.Inpatient,
            }, TestContext.Current.CancellationToken);
        Assert.True(result.IsFailure);
        Assert.NotNull(result.Error);
        Assert.Equal(Status.BadRequest, result.StatusCode);

        // Nothing must be created when the disease does not exist
        Assert.Equal(0, await _context.Causes.CountAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task CreateCause_SoftDeletedDisease_Fail()
    {
        // A soft-deleted disease is hidden by the query filter, so referencing it
        // must be rejected just like an unknown disease
        var (deletedDiseaseId, pathogenId) = await SeedAsync(softDeletedDisease: true);

        var result = await _handler.HandleAsync(
            new CreateCauseCommand
            {
                DiseaseId = deletedDiseaseId,
                PathogenId = pathogenId,
                Severity = Severity.Moderate,
                TreatmentSite = TreatmentSite.Inpatient,
            }, TestContext.Current.CancellationToken);
        Assert.True(result.IsFailure);
        Assert.NotNull(result.Error);
        Assert.Equal(Status.BadRequest, result.StatusCode);
    }

    [Fact]
    public async Task CreateCause_PathogenNotFound_Fail()
    {
        var (diseaseId, _) = await SeedAsync();
        var unknownPathogenId = Guid.CreateVersion7();

        var result = await _handler.HandleAsync(
            new CreateCauseCommand
            {
                DiseaseId = diseaseId,
                PathogenId = unknownPathogenId,
                Severity = Severity.Moderate,
                TreatmentSite = TreatmentSite.Inpatient,
            }, TestContext.Current.CancellationToken);
        Assert.True(result.IsFailure);
        Assert.NotNull(result.Error);
        Assert.Equal(Status.BadRequest, result.StatusCode);

        Assert.Equal(0, await _context.Causes.CountAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task CreateCause_SoftDeletedPathogen_Fail()
    {
        // A soft-deleted pathogen is hidden by the query filter, so referencing it
        // must be rejected just like an unknown pathogen
        var (diseaseId, deletedPathogenId) = await SeedAsync(softDeletedPathogen: true);

        var result = await _handler.HandleAsync(
            new CreateCauseCommand
            {
                DiseaseId = diseaseId,
                PathogenId = deletedPathogenId,
                Severity = Severity.Moderate,
                TreatmentSite = TreatmentSite.Inpatient,
            }, TestContext.Current.CancellationToken);
        Assert.True(result.IsFailure);
        Assert.NotNull(result.Error);
        Assert.Equal(Status.BadRequest, result.StatusCode);
    }

    [Fact]
    public async Task CreateCause_DuplicateTuple_Fail()
    {
        /*
         * Business rule: the exact tuple (disease, pathogen, severity, treatment
         * site) can only exist once among the active causes
         */
        var (diseaseId, pathogenId) = await SeedAsync();
        await _context.Causes.AddAsync(new Cause
        {
            DiseaseId = diseaseId,
            PathogenId = pathogenId,
            Severity = Severity.Mild,
            TreatmentSite = TreatmentSite.Outpatient,
        }, TestContext.Current.CancellationToken);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await _handler.HandleAsync(
            new CreateCauseCommand
            {
                DiseaseId = diseaseId,
                PathogenId = pathogenId,
                Severity = Severity.Mild,
                TreatmentSite = TreatmentSite.Outpatient,
            }, TestContext.Current.CancellationToken);
        Assert.True(result.IsFailure);
        Assert.NotNull(result.Error);
        Assert.Equal(Status.BadRequest, result.StatusCode);

        // The duplicate must not be persisted
        Assert.Equal(1, await _context.Causes.CountAsync(TestContext.Current.CancellationToken));
    }

    # endregion
}
