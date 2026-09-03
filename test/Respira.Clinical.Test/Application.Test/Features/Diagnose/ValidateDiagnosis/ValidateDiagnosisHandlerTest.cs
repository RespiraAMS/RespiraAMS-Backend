using Application.Features.Diagnose.ValidateDiagnosis;
using Domain.Enums;
using Domain.Models;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Respira.ServiceDefaults.Contracts.Results;
using AntibioticModel = Domain.Models.Antibiotic;

namespace Application.Test.Features.Diagnose.ValidateDiagnosis;

public class ValidateDiagnosisHandlerTest : IClassFixture<PostgresFixture>, IAsyncLifetime
{
    private readonly DbContextOptions<AppDbContext> _options;
    private readonly ValidateDiagnosisHandler _handler;
    private readonly AppDbContext _context;

    public ValidateDiagnosisHandlerTest(PostgresFixture fixture)
    {
        _options = new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(fixture.ConnectionString).Options;
        _context = new AppDbContext(_options);

        var logger = new Mock<ILogger<ValidateDiagnosisHandler>>().Object;

        _handler = new(_context, logger);
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

    private async Task<Pathogen> SeedPathogenAsync(string name)
    {
        var pathogen = new Pathogen { Name = name, Description = $"Description for {name}" };
        _context.Pathogens.Add(pathogen);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);
        return pathogen;
    }

    private async Task<AntibioticGroup> SeedGroupAsync(string name)
    {
        var group = new AntibioticGroup { Name = name, Description = name, ParentId = null };
        _context.AntibioticGroups.Add(group);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);
        return group;
    }

    private async Task<AntibioticModel> SeedAntibioticAsync(
        string name,
        AntibioticGroup group,
        AwareClassification classification,
        List<(RouteOfAdministration route, string dose)> dosages)
    {
        var antibiotic = new AntibioticModel
        {
            Name = name,
            AntibioticGroupId = group.Id,
            AntibioticGroup = group,
            Classification = classification,
        };
        _context.Antibiotics.Add(antibiotic);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        foreach (var (route, dose) in dosages)
        {
            _context.Dosages.Add(new Dosage
            {
                AntibioticId = antibiotic.Id,
                RouteOfAdministration = route,
                Dose = dose,
            });
        }

        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);
        return antibiotic;
    }

    private static ValidateDiagnosisQuery BuildQuery(
        List<AntibioticRecord>? antibiotics = null,
        List<PathogenRecord>? pathogens = null,
        string? severity = null,
        string? treatmentSite = null)
    {
        return new ValidateDiagnosisQuery
        {
            Antibiotics = antibiotics ?? [],
            Pathogens = pathogens ?? [],
            Severity = severity,
            TreatmentSite = treatmentSite,
        };
    }

    # region Happy path

    [Fact]
    public async Task ValidateDiagnosis_AllValidEnumsAndRecords_ReturnsTrue()
    {
        await CleanupAsync();
        var pneumo = await SeedPathogenAsync("Streptococcus pneumoniae");
        var group = await SeedGroupAsync("Beta-lactams");
        var amox = await SeedAntibioticAsync("Amoxicillin", group, AwareClassification.Access,
            [(RouteOfAdministration.Oral, "500 mg every 8 hours")]);

        var query = BuildQuery(
            antibiotics: [new AntibioticRecord
            {
                Id = amox.Id,
                Name = "Amoxicillin",
                Classification = nameof(AwareClassification.Access),
                RouteOfAdministration = nameof(RouteOfAdministration.Oral),
                Dose = "500 mg every 8 hours",
            }],
            pathogens: [new PathogenRecord { Id = pneumo.Id, Name = "Streptococcus pneumoniae" }],
            severity: nameof(Severity.Mild),
            treatmentSite: nameof(TreatmentSite.Outpatient));

        var result = await _handler.HandleAsync(query, TestContext.Current.CancellationToken);
        Assert.True(result.IsSuccess());
        Assert.NotNull(result.Data);
        Assert.Null(result.Error);
        Assert.Equal(Status.Success, result.StatusCode);

        // Business rule: every provided record matches a real DB record (name + classification + dose)
        // and the optional enums are recognised -> the diagnosis payload is valid
        Assert.True(result.Data.IsValid);
    }

    [Fact]
    public async Task ValidateDiagnosis_NullOptionalEnums_ReturnsTrue()
    {
        await CleanupAsync();
        var haemophilus = await SeedPathogenAsync("Haemophilus influenzae");
        var group = await SeedGroupAsync("Macrolides");
        var azith = await SeedAntibioticAsync("Azithromycin", group, AwareClassification.Watch,
            [(RouteOfAdministration.Oral, "500 mg on day 1, then 250 mg daily")]);

        // Severity and TreatmentSite are optional -> omit them
        var query = BuildQuery(
            antibiotics: [new AntibioticRecord
            {
                Id = azith.Id,
                Name = "Azithromycin",
                Classification = nameof(AwareClassification.Watch),
                RouteOfAdministration = nameof(RouteOfAdministration.Oral),
                Dose = "500 mg on day 1, then 250 mg daily",
            }],
            pathogens: [new PathogenRecord { Id = haemophilus.Id, Name = "Haemophilus influenzae" }]);

        var result = await _handler.HandleAsync(query, TestContext.Current.CancellationToken);
        Assert.True(result.IsSuccess());
        Assert.NotNull(result.Data);
        Assert.Null(result.Error);
        Assert.Equal(Status.Success, result.StatusCode);

        Assert.True(result.Data.IsValid);
    }

    [Fact]
    public async Task ValidateDiagnosis_CaseInsensitiveNamesAndEnums_ReturnsTrue()
    {
        await CleanupAsync();
        var staph = await SeedPathogenAsync("Staphylococcus aureus");
        var group = await SeedGroupAsync("Reserve carbapenems");
        var meropenem = await SeedAntibioticAsync("Meropenem", group, AwareClassification.Reserve,
            [(RouteOfAdministration.Intravenous, "1 g every 8 hours")]);

        // Lower-case names / enums must still match the DB records (EqualsIgnoreCase on names,
        // ignoreCase parse on enum strings)
        var query = BuildQuery(
            antibiotics: [new AntibioticRecord
            {
                Id = meropenem.Id,
                Name = "meropenem",
                Classification = "reserve",
                RouteOfAdministration = "intravenous",
                Dose = "1 g every 8 hours",
            }],
            pathogens: [new PathogenRecord { Id = staph.Id, Name = "staphylococcus aureus" }],
            severity: "severe",
            treatmentSite: "inpatient");

        var result = await _handler.HandleAsync(query, TestContext.Current.CancellationToken);
        Assert.True(result.IsSuccess());
        Assert.NotNull(result.Data);
        Assert.Null(result.Error);
        Assert.Equal(Status.Success, result.StatusCode);

        // Business rule: matching is case-insensitive for pathogen/antibiotic names and enum strings
        Assert.True(result.Data.IsValid);
    }

    [Fact]
    public async Task ValidateDiagnosis_MultiplePathogensAndAntibiotics_ReturnsTrue()
    {
        await CleanupAsync();
        var pneumo = await SeedPathogenAsync("Streptococcus pneumoniae");
        var klebsiella = await SeedPathogenAsync("Klebsiella pneumoniae");
        var group = await SeedGroupAsync("Beta-lactams");
        var ceftriaxone = await SeedAntibioticAsync("Ceftriaxone", group, AwareClassification.Watch,
            [(RouteOfAdministration.Intravenous, "2 g once daily")]);
        var group2 = await SeedGroupAsync("Respiratory fluoroquinolones");
        var levo = await SeedAntibioticAsync("Levofloxacin", group2, AwareClassification.Watch,
            [(RouteOfAdministration.Oral, "750 mg once daily"), (RouteOfAdministration.Intravenous, "750 mg once daily")]);

        var query = BuildQuery(
            antibiotics:
            [
                new AntibioticRecord
                {
                    Id = ceftriaxone.Id,
                    Name = "Ceftriaxone",
                    Classification = nameof(AwareClassification.Watch),
                    RouteOfAdministration = nameof(RouteOfAdministration.Intravenous),
                    Dose = "2 g once daily",
                },
                new AntibioticRecord
                {
                    Id = levo.Id,
                    Name = "Levofloxacin",
                    Classification = nameof(AwareClassification.Watch),
                    RouteOfAdministration = nameof(RouteOfAdministration.Intravenous),
                    Dose = "750 mg once daily",
                },
            ],
            pathogens:
            [
                new PathogenRecord { Id = pneumo.Id, Name = "Streptococcus pneumoniae" },
                new PathogenRecord { Id = klebsiella.Id, Name = "Klebsiella pneumoniae" },
            ],
            severity: nameof(Severity.Severe),
            treatmentSite: nameof(TreatmentSite.IntensiveCareUnit));

        var result = await _handler.HandleAsync(query, TestContext.Current.CancellationToken);
        Assert.True(result.IsSuccess());
        Assert.NotNull(result.Data);
        Assert.Null(result.Error);
        Assert.Equal(Status.Success, result.StatusCode);

        Assert.True(result.Data.IsValid);
    }

    # endregion

    # region Fail path - Invalid optional enums

    [Fact]
    public async Task ValidateDiagnosis_InvalidSeverity_ReturnsFalse()
    {
        await CleanupAsync();
        // Invalid enum value expressed by casting an out-of-range integer to the enum (req 11)
        var invalidSeverity = ((Severity)999).ToString();

        var query = BuildQuery(severity: invalidSeverity);

        var result = await _handler.HandleAsync(query, TestContext.Current.CancellationToken);
        Assert.True(result.IsSuccess());
        Assert.NotNull(result.Data);
        Assert.Null(result.Error);
        Assert.Equal(Status.Success, result.StatusCode);

        // Business rule: an unrecognised severity string must make the payload invalid
        Assert.False(result.Data.IsValid);
    }

    [Fact]
    public async Task ValidateDiagnosis_InvalidTreatmentSite_ReturnsFalse()
    {
        await CleanupAsync();
        var invalidSite = ((TreatmentSite)999).ToString();

        var query = BuildQuery(treatmentSite: invalidSite);

        var result = await _handler.HandleAsync(query, TestContext.Current.CancellationToken);
        Assert.True(result.IsSuccess());
        Assert.NotNull(result.Data);
        Assert.Null(result.Error);
        Assert.Equal(Status.Success, result.StatusCode);

        Assert.False(result.Data.IsValid);
    }

    # endregion

    # region Fail path - Pathogen validation

    [Fact]
    public async Task ValidateDiagnosis_PathogenNotFound_ReturnsFalse()
    {
        await CleanupAsync();
        // No pathogen seeded with this id
        var unknownId = Guid.CreateVersion7();

        var query = BuildQuery(
            pathogens: [new PathogenRecord { Id = unknownId, Name = "Streptococcus pneumoniae" }]);

        var result = await _handler.HandleAsync(query, TestContext.Current.CancellationToken);
        Assert.True(result.IsSuccess());
        Assert.NotNull(result.Data);
        Assert.Null(result.Error);
        Assert.Equal(Status.Success, result.StatusCode);

        // Business rule: every referenced pathogen must exist in the database
        Assert.False(result.Data.IsValid);
    }

    [Fact]
    public async Task ValidateDiagnosis_PathogenNameMismatch_ReturnsFalse()
    {
        await CleanupAsync();
        var pneumo = await SeedPathogenAsync("Streptococcus pneumoniae");

        // Id exists, but the provided name does not match the stored record
        var query = BuildQuery(
            pathogens: [new PathogenRecord { Id = pneumo.Id, Name = "Mycoplasma pneumoniae" }]);

        var result = await _handler.HandleAsync(query, TestContext.Current.CancellationToken);
        Assert.True(result.IsSuccess());
        Assert.NotNull(result.Data);
        Assert.Null(result.Error);
        Assert.Equal(Status.Success, result.StatusCode);

        // Business rule: the pathogen name must match the stored record (case-insensitive)
        Assert.False(result.Data.IsValid);
    }

    # endregion

    # region Fail path - Antibiotic validation

    [Fact]
    public async Task ValidateDiagnosis_AntibioticNotFound_ReturnsFalse()
    {
        await CleanupAsync();
        var unknownId = Guid.CreateVersion7();
        _ = await SeedGroupAsync("Beta-lactams");

        var query = BuildQuery(
            antibiotics: [new AntibioticRecord
            {
                Id = unknownId,
                Name = "Amoxicillin",
                Classification = nameof(AwareClassification.Access),
                RouteOfAdministration = nameof(RouteOfAdministration.Oral),
                Dose = "500 mg every 8 hours",
            }]);

        var result = await _handler.HandleAsync(query, TestContext.Current.CancellationToken);
        Assert.True(result.IsSuccess());
        Assert.NotNull(result.Data);
        Assert.Null(result.Error);
        Assert.Equal(Status.Success, result.StatusCode);

        // Business rule: every referenced antibiotic must exist in the database
        Assert.False(result.Data.IsValid);
    }

    [Fact]
    public async Task ValidateDiagnosis_AntibioticNameMismatch_ReturnsFalse()
    {
        await CleanupAsync();
        var group = await SeedGroupAsync("Beta-lactams");
        var amox = await SeedAntibioticAsync("Amoxicillin", group, AwareClassification.Access,
            [(RouteOfAdministration.Oral, "500 mg every 8 hours")]);

        // Id matches, but name does not
        var query = BuildQuery(
            antibiotics: [new AntibioticRecord
            {
                Id = amox.Id,
                Name = "Amoxycillin",
                Classification = nameof(AwareClassification.Access),
                RouteOfAdministration = nameof(RouteOfAdministration.Oral),
                Dose = "500 mg every 8 hours",
            }]);

        var result = await _handler.HandleAsync(query, TestContext.Current.CancellationToken);
        Assert.True(result.IsSuccess());
        Assert.NotNull(result.Data);
        Assert.Null(result.Error);
        Assert.Equal(Status.Success, result.StatusCode);

        // Business rule: the antibiotic name must match the stored record (case-insensitive)
        Assert.False(result.Data.IsValid);
    }

    [Fact]
    public async Task ValidateDiagnosis_InvalidRouteOfAdministration_ReturnsFalse()
    {
        await CleanupAsync();
        var group = await SeedGroupAsync("Beta-lactams");
        var amox = await SeedAntibioticAsync("Amoxicillin", group, AwareClassification.Access,
            [(RouteOfAdministration.Oral, "500 mg every 8 hours")]);

        // Out-of-range integer cast to the enum (req 11) -> unparseable route string
        var invalidRoute = ((RouteOfAdministration)999).ToString();

        var query = BuildQuery(
            antibiotics: [new AntibioticRecord
            {
                Id = amox.Id,
                Name = "Amoxicillin",
                Classification = nameof(AwareClassification.Access),
                RouteOfAdministration = invalidRoute,
                Dose = "500 mg every 8 hours",
            }]);

        var result = await _handler.HandleAsync(query, TestContext.Current.CancellationToken);
        Assert.True(result.IsSuccess());
        Assert.NotNull(result.Data);
        Assert.Null(result.Error);
        Assert.Equal(Status.Success, result.StatusCode);

        // Business rule: route of administration must be a recognised value
        Assert.False(result.Data.IsValid);
    }

    [Fact]
    public async Task ValidateDiagnosis_InvalidClassification_ReturnsFalse()
    {
        await CleanupAsync();
        var group = await SeedGroupAsync("Beta-lactams");
        var amox = await SeedAntibioticAsync("Amoxicillin", group, AwareClassification.Access,
            [(RouteOfAdministration.Oral, "500 mg every 8 hours")]);

        var invalidClassification = ((AwareClassification)999).ToString();

        var query = BuildQuery(
            antibiotics: [new AntibioticRecord
            {
                Id = amox.Id,
                Name = "Amoxicillin",
                Classification = invalidClassification,
                RouteOfAdministration = nameof(RouteOfAdministration.Oral),
                Dose = "500 mg every 8 hours",
            }]);

        var result = await _handler.HandleAsync(query, TestContext.Current.CancellationToken);
        Assert.True(result.IsSuccess());
        Assert.NotNull(result.Data);
        Assert.Null(result.Error);
        Assert.Equal(Status.Success, result.StatusCode);

        // Business rule: classification must be a recognised value
        Assert.False(result.Data.IsValid);
    }

    [Fact]
    public async Task ValidateDiagnosis_ClassificationMismatch_ReturnsFalse()
    {
        await CleanupAsync();
        var group = await SeedGroupAsync("Beta-lactams");
        var amox = await SeedAntibioticAsync("Amoxicillin", group, AwareClassification.Access,
            [(RouteOfAdministration.Oral, "500 mg every 8 hours")]);

        // Provided classification is valid but differs from the stored AWaRe category
        var query = BuildQuery(
            antibiotics: [new AntibioticRecord
            {
                Id = amox.Id,
                Name = "Amoxicillin",
                Classification = nameof(AwareClassification.Watch),
                RouteOfAdministration = nameof(RouteOfAdministration.Oral),
                Dose = "500 mg every 8 hours",
            }]);

        var result = await _handler.HandleAsync(query, TestContext.Current.CancellationToken);
        Assert.True(result.IsSuccess());
        Assert.NotNull(result.Data);
        Assert.Null(result.Error);
        Assert.Equal(Status.Success, result.StatusCode);

        // Business rule: the supplied classification must equal the stored classification
        Assert.False(result.Data.IsValid);
    }

    [Fact]
    public async Task ValidateDiagnosis_DoseMismatch_ReturnsFalse()
    {
        await CleanupAsync();
        var group = await SeedGroupAsync("Beta-lactams");
        var amox = await SeedAntibioticAsync("Amoxicillin", group, AwareClassification.Access,
            [(RouteOfAdministration.Oral, "500 mg every 8 hours")]);

        // Dose string does not match any stored dosage for the antibiotic
        var query = BuildQuery(
            antibiotics: [new AntibioticRecord
            {
                Id = amox.Id,
                Name = "Amoxicillin",
                Classification = nameof(AwareClassification.Access),
                RouteOfAdministration = nameof(RouteOfAdministration.Oral),
                Dose = "1000 mg every 8 hours",
            }]);

        var result = await _handler.HandleAsync(query, TestContext.Current.CancellationToken);
        Assert.True(result.IsSuccess());
        Assert.NotNull(result.Data);
        Assert.Null(result.Error);
        Assert.Equal(Status.Success, result.StatusCode);

        // Business rule: the dose must correspond to a real dosage of the antibiotic (case-insensitive)
        Assert.False(result.Data.IsValid);
    }

    [Fact]
    public async Task ValidateDiagnosis_RouteMismatch_ReturnsFalse()
    {
        await CleanupAsync();
        var group = await SeedGroupAsync("Beta-lactams");
        // Antibiotic only has an oral dosage
        var amox = await SeedAntibioticAsync("Amoxicillin", group, AwareClassification.Access,
            [(RouteOfAdministration.Oral, "500 mg every 8 hours")]);

        // Route is valid but no dosage with that route exists for the antibiotic
        var query = BuildQuery(
            antibiotics: [new AntibioticRecord
            {
                Id = amox.Id,
                Name = "Amoxicillin",
                Classification = nameof(AwareClassification.Access),
                RouteOfAdministration = nameof(RouteOfAdministration.Intravenous),
                Dose = "500 mg every 8 hours",
            }]);

        var result = await _handler.HandleAsync(query, TestContext.Current.CancellationToken);
        Assert.True(result.IsSuccess());
        Assert.NotNull(result.Data);
        Assert.Null(result.Error);
        Assert.Equal(Status.Success, result.StatusCode);

        // Business rule: a dosage must exist for the requested route of administration
        Assert.False(result.Data.IsValid);
    }

    # endregion
}
