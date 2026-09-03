using Application.Features.Diagnose.EmpiricalDiagnose;
using Domain.Enums;
using Domain.Models;
using Domain.Services.Dtos;
using Domain.Services.Implementations;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using AntibioticModel = Domain.Models.Antibiotic;
using Range = Domain.Models.Range;
using Respira.ServiceDefaults.Contracts.Results;

namespace Application.Test.Features.Diagnose.EmpiricalDiagnose;

public class EmpiricalDiagnoseHandlerTest : IClassFixture<PostgresFixture>, IAsyncLifetime
{
    private readonly DbContextOptions<AppDbContext> _options;
    private readonly EmpiricalDiagnoseHandler _handler;
    private readonly AppDbContext _context;

    public EmpiricalDiagnoseHandlerTest(PostgresFixture fixture)
    {
        _options = new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(fixture.ConnectionString).Options;
        _context = new AppDbContext(_options);

        // IDiagnoseService is registered as CapDiagnoseService (the previously untested path)
        var service = new CapDiagnoseService(
            new Mock<ILogger<CapDiagnoseService>>().Object,
            Options.Create(new TuningCoefficient()));
        var patientInfoMapper = new EmpiricalDiagnosePatientInfoMapper();
        var clinicalPictureMapper = new EmpiricalDiagnoseClinicalPictureMapper();
        var logger = new Mock<ILogger<EmpiricalDiagnoseHandler>>().Object;

        _handler = new(_context, service, patientInfoMapper, clinicalPictureMapper, logger);
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
        _context.EmpiricTreatmentProtocols.RemoveRange(
            await _context.EmpiricTreatmentProtocols.IgnoreQueryFilters().ToListAsync(TestContext.Current.CancellationToken));
        _context.ResistanceRiskFactors.RemoveRange(
            await _context.ResistanceRiskFactors.IgnoreQueryFilters().ToListAsync(TestContext.Current.CancellationToken));
        _context.IcuHospitalizeCriteria.RemoveRange(
            await _context.IcuHospitalizeCriteria.IgnoreQueryFilters().ToListAsync(TestContext.Current.CancellationToken));
        _context.Diseases.RemoveRange(
            await _context.Diseases.IgnoreQueryFilters().ToListAsync(TestContext.Current.CancellationToken));
        _context.Dosages.RemoveRange(
            await _context.Dosages.IgnoreQueryFilters().ToListAsync(TestContext.Current.CancellationToken));
        _context.Antibiotics.RemoveRange(
            await _context.Antibiotics.IgnoreQueryFilters().ToListAsync(TestContext.Current.CancellationToken));
        _context.AntibioticGroups.RemoveRange(
            await _context.AntibioticGroups.IgnoreQueryFilters().ToListAsync(TestContext.Current.CancellationToken));
        _context.Pathogens.RemoveRange(
            await _context.Pathogens.IgnoreQueryFilters().ToListAsync(TestContext.Current.CancellationToken));
        _context.Criteria.RemoveRange(
            await _context.Criteria.IgnoreQueryFilters().ToListAsync(TestContext.Current.CancellationToken));
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    private async Task<Pathogen> SeedPathogenAsync(string name = "Streptococcus pneumoniae")
    {
        var pathogen = new Pathogen { Name = name, Description = $"Description for {name}" };
        _context.Pathogens.Add(pathogen);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);
        return pathogen;
    }

    private async Task<BooleanCriterion> SeedCriterionAsync(string name)
    {
        var criterion = new BooleanCriterion { Name = name };
        _context.Criteria.Add(criterion);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);
        return criterion;
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
        List<(RouteOfAdministration route, string dose, Range? crcl)> dosages)
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

    private async Task<Disease> SeedDiseaseAsync(string name, int threshold)
    {
        var disease = new Disease
        {
            Name = name,
            Description = $"Description for {name}",
            IcuScoreThreshold = threshold,
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

    private async Task SeedResistanceRiskFactorAsync(Guid diseaseId, Pathogen pathogen, Criterion criterion)
    {
        _context.ResistanceRiskFactors.Add(new ResistanceRiskFactor
        {
            DiseaseId = diseaseId,
            PathogenId = pathogen.Id,
            Pathogen = pathogen,
            CriterionId = criterion.Id,
            Criterion = criterion,
            Name = "Prior antibiotic use within 90 days",
        });
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    private async Task<EmpiricTreatmentProtocol> SeedProtocolAsync(
        Guid diseaseId, Severity severity, TreatmentSite treatmentSite, List<AntibioticModel> medicines)
    {
        var protocol = new EmpiricTreatmentProtocol
        {
            Name = "IDSA/ATS 2024 CAP Empiric Guidance",
            Issuer = "Infectious Diseases Society of America",
            IssueDate = new DateOnly(2024, 8, 1),
            Version = 3,
            DiseaseId = diseaseId,
            Severity = severity,
            TreatmentSite = treatmentSite,
            Medicines = medicines,
        };
        _context.EmpiricTreatmentProtocols.Add(protocol);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);
        return protocol;
    }

    private static EmpiricalDiagnoseQuery BuildQuery(
        Guid diseaseId,
        List<Guid> icu,
        List<Guid> rrf,
        List<Guid> other,
        int ageYears,
        bool confusion = false,
        decimal? urea = 5m,
        int respiratory = 20,
        decimal systolic = 120m,
        decimal diastolic = 80m)
    {
        return new EmpiricalDiagnoseQuery
        {
            DiseaseId = diseaseId,
            DateOfBirth = DateOnly.FromDateTime(DateTime.UtcNow).AddYears(-ageYears),
            IsMale = true,
            Weight = 70m,
            Height = 1.7m,
            SerumCreatine = 1.0m,
            Confusion = confusion,
            Urea = urea,
            Respiratory = respiratory,
            SystolicBloodPressure = systolic,
            DiastolicBloodPressure = diastolic,
            IcuHospitalizeCriteria = icu,
            ResistanceRiskFactors = rrf,
            OtherCriteria = other,
        };
    }

    # region Happy path

    [Fact]
    public async Task EmpiricalDiagnose_ReturnsFullResult_Success()
    {
        await CleanupAsync();
        var pathogen = await SeedPathogenAsync();
        var rrfCrit = await SeedCriterionAsync("Resistance criterion");
        var group = await SeedGroupAsync("Beta-lactams");
        var amox = await SeedAntibioticAsync("Amoxicillin", group, AwareClassification.Access,
            [(RouteOfAdministration.Oral, "500 mg every 8 hours", null)]);
        var disease = await SeedDiseaseAsync("Community-Acquired Pneumonia", threshold: 2);
        await SeedResistanceRiskFactorAsync(disease.Id, pathogen, rrfCrit);
        await SeedProtocolAsync(disease.Id, Severity.Mild, TreatmentSite.Outpatient, [amox]);

        // 50yo male, 70kg, 1.7m, scr 1.0 -> CrCl = 87.5; CURB-65 score 0 -> Mild/Outpatient
        var query = BuildQuery(disease.Id, [], [rrfCrit.Id], [rrfCrit.Id], ageYears: 50);

        var result = await _handler.HandleAsync(query, TestContext.Current.CancellationToken);
        Assert.True(result.IsSuccess());
        Assert.Null(result.Error);
        Assert.NotNull(result.Data);
        Assert.Equal(Status.Success, result.StatusCode);

        // Business rule: CrCl computed (Cockcroft-Gault) and positive
        Assert.Equal(87.5m, result.Data.Crcl);

        // Business rule: CURB-65 severity/treatment site
        Assert.Equal(Severity.Mild, result.Data.Severity);
        Assert.Equal(TreatmentSite.Outpatient, result.Data.TreatmentSite);

        // Business rule: recommended medicines (one per group) are projected with their group
        var medicine = Assert.Single(result.Data.Medicines);
        Assert.Equal(amox.Id, medicine.Id);
        Assert.Equal("Amoxicillin", medicine.Name);
        Assert.Equal(AwareClassification.Access, medicine.Classification);
        Assert.Equal(group.Id, medicine.AntibioticGroupId);
        Assert.Equal("Beta-lactams", medicine.AntibioticGroupName);
        Assert.NotEmpty(medicine.Dosages);

        // Business rule: recommendations include all medicines across reference protocols
        var recommendation = Assert.Single(result.Data.Recommendations);
        Assert.Equal(amox.Id, recommendation.Id);

        // Business rule: infection probabilities computed from resistance risk factors
        var prob = Assert.Single(result.Data.InfectionProbabilities);
        Assert.Equal(pathogen.Id, prob.PathogenId);
        Assert.Equal(1.0m, prob.Probability);

        // Business rule: matched protocols are returned as references
        var reference = Assert.Single(result.Data.References);
        Assert.Equal("IDSA/ATS 2024 CAP Empiric Guidance", reference.Name);
        Assert.Equal(3, reference.Version);
    }

    [Fact]
    public async Task EmpiricalDiagnose_SevereViaCurb65_Success()
    {
        await CleanupAsync();
        var group = await SeedGroupAsync("Beta-lactams");
        var amox = await SeedAntibioticAsync("Amoxicillin", group, AwareClassification.Access,
            [(RouteOfAdministration.Oral, "500 mg every 8 hours", null)]);
        var disease = await SeedDiseaseAsync("Community-Acquired Pneumonia", threshold: 2);
        await SeedProtocolAsync(disease.Id, Severity.Severe, TreatmentSite.IntensiveCareUnit, [amox]);

        // CURB-65 score 5 (confusion, urea>7, resp>=30, bp low, age>=65) -> Severe/ICU
        var query = BuildQuery(disease.Id, [], [], [], ageYears: 70,
            confusion: true, urea: 8m, respiratory: 32, systolic: 80m, diastolic: 50m);

        var result = await _handler.HandleAsync(query, TestContext.Current.CancellationToken);
        Assert.True(result.IsSuccess());
        Assert.Null(result.Error);
        Assert.NotNull(result.Data);
        Assert.Equal(Status.Success, result.StatusCode);

        Assert.Equal(Severity.Severe, result.Data.Severity);
        Assert.Equal(TreatmentSite.IntensiveCareUnit, result.Data.TreatmentSite);
        Assert.NotEmpty(result.Data.Medicines);
    }

    [Fact]
    public async Task EmpiricalDiagnose_UpgradesToIcuWhenCriteriaMet_Success()
    {
        await CleanupAsync();
        var icuCrit = await SeedCriterionAsync("ICU criterion");
        var group = await SeedGroupAsync("Beta-lactams");
        var amox = await SeedAntibioticAsync("Amoxicillin", group, AwareClassification.Access,
            [(RouteOfAdministration.Oral, "500 mg every 8 hours", null)]);
        // High ICU threshold; the single ICU criterion's score (5) meets it
        var disease = await SeedDiseaseAsync("Community-Acquired Pneumonia", threshold: 3);
        await SeedIcuCriterionAsync(disease.Id, icuCrit, score: 5);
        await SeedProtocolAsync(disease.Id, Severity.Mild, TreatmentSite.Outpatient, [amox]);

        // CURB-65 score 0 (Mild/Outpatient) but the patient meets the ICU criteria -> upgraded to ICU
        var query = BuildQuery(disease.Id, [icuCrit.Id], [], [], ageYears: 50);

        var result = await _handler.HandleAsync(query, TestContext.Current.CancellationToken);
        Assert.True(result.IsSuccess());
        Assert.Null(result.Error);
        Assert.NotNull(result.Data);
        Assert.Equal(Status.Success, result.StatusCode);

        Assert.Equal(Severity.Mild, result.Data.Severity);
        Assert.Equal(TreatmentSite.IntensiveCareUnit, result.Data.TreatmentSite);
    }

    [Fact]
    public async Task EmpiricalDiagnose_EmptyCriteriaLists_Success()
    {
        await CleanupAsync();
        var group = await SeedGroupAsync("Beta-lactams");
        var amox = await SeedAntibioticAsync("Amoxicillin", group, AwareClassification.Access,
            [(RouteOfAdministration.Oral, "500 mg every 8 hours", null)]);
        var disease = await SeedDiseaseAsync("Community-Acquired Pneumonia", threshold: 2);
        await SeedProtocolAsync(disease.Id, Severity.Mild, TreatmentSite.Outpatient, [amox]);

        // All criteria lists empty: the subset-validation checks pass on empty collections
        var query = BuildQuery(disease.Id, [], [], [], ageYears: 50);

        var result = await _handler.HandleAsync(query, TestContext.Current.CancellationToken);
        Assert.True(result.IsSuccess());
        Assert.Null(result.Error);
        Assert.NotNull(result.Data);
        Assert.Equal(Status.Success, result.StatusCode);

        Assert.Equal(Severity.Mild, result.Data.Severity);
        Assert.Empty(result.Data.InfectionProbabilities);
        Assert.NotEmpty(result.Data.Medicines);
    }

    # endregion

    # region Fail path

    [Fact]
    public async Task EmpiricalDiagnose_DiseaseNotFound_Fail()
    {
        await CleanupAsync();
        var unknownId = Guid.CreateVersion7();

        var query = BuildQuery(unknownId, [], [], [], ageYears: 50);
        var result = await _handler.HandleAsync(query, TestContext.Current.CancellationToken);
        Assert.True(result.IsFailure());
        Assert.NotNull(result.Error);
        Assert.Equal(Status.ResourceNotFound, result.StatusCode);
    }

    [Fact]
    public async Task EmpiricalDiagnose_IcuCriteriaNotSubset_Fail()
    {
        await CleanupAsync();
        var icuCrit = await SeedCriterionAsync("ICU criterion");
        var disease = await SeedDiseaseAsync("Community-Acquired Pneumonia", threshold: 2);
        await SeedIcuCriterionAsync(disease.Id, icuCrit, score: 2);

        // Provided ICU criterion ID does not belong to this disease
        var query = BuildQuery(disease.Id, [Guid.CreateVersion7()], [], [], ageYears: 50);

        var result = await _handler.HandleAsync(query, TestContext.Current.CancellationToken);
        Assert.True(result.IsFailure());
        Assert.NotNull(result.Error);
        Assert.Equal(Status.BadRequest, result.StatusCode);
    }

    [Fact]
    public async Task EmpiricalDiagnose_ResistanceRiskFactorsNotSubset_Fail()
    {
        await CleanupAsync();
        var pathogen = await SeedPathogenAsync();
        var rrfCrit = await SeedCriterionAsync("Resistance criterion");
        var disease = await SeedDiseaseAsync("Community-Acquired Pneumonia", threshold: 2);
        await SeedResistanceRiskFactorAsync(disease.Id, pathogen, rrfCrit);

        var query = BuildQuery(disease.Id, [], [Guid.CreateVersion7()], [], ageYears: 50);

        var result = await _handler.HandleAsync(query, TestContext.Current.CancellationToken);
        Assert.True(result.IsFailure());
        Assert.NotNull(result.Error);
        Assert.Equal(Status.BadRequest, result.StatusCode);
    }

    [Fact]
    public async Task EmpiricalDiagnose_OtherCriteriaNotExisting_Fail()
    {
        await CleanupAsync();
        var disease = await SeedDiseaseAsync("Community-Acquired Pneumonia", threshold: 2);

        // Other criterion ID does not exist in the Criteria table at all
        var query = BuildQuery(disease.Id, [], [], [Guid.CreateVersion7()], ageYears: 50);

        var result = await _handler.HandleAsync(query, TestContext.Current.CancellationToken);
        Assert.True(result.IsFailure());
        Assert.NotNull(result.Error);
        Assert.Equal(Status.BadRequest, result.StatusCode);
    }

    # endregion
}
