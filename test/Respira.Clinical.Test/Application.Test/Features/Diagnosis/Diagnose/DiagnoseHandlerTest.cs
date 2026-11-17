using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Respira.Clinical.Application.Contracts.Data;
using Respira.Clinical.Application.Features.Diagnosis.Diagnose;
using Respira.Clinical.Domain.Entities;
using Respira.Clinical.Domain.Enums;
using Respira.Clinical.Domain.Models;
using Respira.Clinical.Domain.Services;
using Respira.Clinical.Infrastructure.Data;
using Respira.ServiceDefaults.Contracts.Results;
using DiagnosisModel = Respira.Clinical.Domain.Models.Diagnosis;
using MicrobialTestResultModel = Respira.Clinical.Domain.Models.MicrobialTestResult;
using Range = Respira.Clinical.Domain.Models.Range;

namespace Respira.Clinical.Application.Test.Features.Diagnosis.Diagnose
{
    public class DiagnoseHandlerTest : IClassFixture<PostgresFixture>, IAsyncLifetime
    {
        private readonly DbContextOptions<ClinicalDbContext> _options;
        private readonly DiagnoseHandler _handler;
        private readonly IDbContext _context;
        private readonly Mock<IDiagnoseService> _service;

        public DiagnoseHandlerTest(PostgresFixture fixture)
        {
            // Create handler dependencies
            _options = new DbContextOptionsBuilder<ClinicalDbContext>()
                .UseNpgsql(fixture.ConnectionString).Options;
            _context = new ClinicalDbContext(_options);
            // Strict so an unexpected service call (a missing short-circuit) fails the test
            _service = new Mock<IDiagnoseService>(MockBehavior.Strict);
            var logger = new Mock<ILogger<DiagnoseHandler>>().Object;

            // Initialize handler
            _handler = new(_context, _service.Object, logger);
        }

        public async ValueTask DisposeAsync()
        {
            await _context.DisposeAsync();
        }

        public async ValueTask InitializeAsync()
        {
            // Clear leftover data (children first for FK constraints) so every test
            // starts from a known state. IgnoreQueryFilters is needed because soft
            // deleted rows are hidden by the query filter but still occupy the table
            await _context.Treatments.IgnoreQueryFilters()
                .ExecuteDeleteAsync(TestContext.Current.CancellationToken);
            await _context.Antibiograms.IgnoreQueryFilters()
                .ExecuteDeleteAsync(TestContext.Current.CancellationToken);
            await _context.TreatmentProtocols.IgnoreQueryFilters()
                .ExecuteDeleteAsync(TestContext.Current.CancellationToken);
            await _context.RiskFactors.IgnoreQueryFilters()
                .ExecuteDeleteAsync(TestContext.Current.CancellationToken);
            await _context.MetricRules.IgnoreQueryFilters()
                .ExecuteDeleteAsync(TestContext.Current.CancellationToken);
            await _context.SuspectedCauses.IgnoreQueryFilters()
                .ExecuteDeleteAsync(TestContext.Current.CancellationToken);
            await _context.Dosages.IgnoreQueryFilters()
                .ExecuteDeleteAsync(TestContext.Current.CancellationToken);
            await _context.Antibiotics.IgnoreQueryFilters()
                .ExecuteDeleteAsync(TestContext.Current.CancellationToken);
            // A group can reference its parent, so delete sub-groups before parents
            await _context.AntibioticGroups.IgnoreQueryFilters()
                .Where(x => x.ParentId != null)
                .ExecuteDeleteAsync(TestContext.Current.CancellationToken);
            await _context.AntibioticGroups.IgnoreQueryFilters()
                .ExecuteDeleteAsync(TestContext.Current.CancellationToken);
            await _context.Pathogens.IgnoreQueryFilters()
                .ExecuteDeleteAsync(TestContext.Current.CancellationToken);
            await _context.Criteria.IgnoreQueryFilters()
                .ExecuteDeleteAsync(TestContext.Current.CancellationToken);
            await _context.ClinicalMetrics.IgnoreQueryFilters()
                .ExecuteDeleteAsync(TestContext.Current.CancellationToken);
            await _context.ClinicalVariables.IgnoreQueryFilters()
                .ExecuteDeleteAsync(TestContext.Current.CancellationToken);
        }

        #region Seed helpers

        private sealed record SeedData(
            NumericClinicalVariable Temperature,
            BooleanClinicalVariable Female,
            CategoricalClinicalVariable Sars,
            NumericClinicalVariable Age,
            Antibiotic Amoxicillin,
            Antibiotic Azithromycin,
            Antibiotic Meropenem,
            Pathogen Pneumococcus,
            Pathogen Mycoplasma,
            Pathogen Pseudomonas,
            TreatmentProtocol AppliedProtocol,
            ClinicalMetric AppliedMetric,
            Criterion ProtocolCriterion);

        private static Range NewRange(decimal min, decimal max, string? unit)
        {
            return new Range
            {
                Min = min,
                Max = max,
                Unit = unit,
                IsMinExclusive = false,
                IsMaxExclusive = false,
            };
        }

        private static NumericClinicalVariable Numeric(
            string name,
            string code,
            string description,
            ClinicalVariableCategory category,
            string unit,
            Range acceptedRange,
            Range? normalRange = null)
        {
            return new NumericClinicalVariable
            {
                Code = code,
                Name = name,
                Description = description,
                CanonicalUnit = unit,
                IsRequired = true,
                Category = category,
                AcceptedRange = acceptedRange,
                NormalRange = normalRange,
            };
        }

        private static BooleanClinicalVariable Boolean(
            string name,
            string code,
            string description,
            ClinicalVariableCategory category,
            bool isRequired)
        {
            return new BooleanClinicalVariable
            {
                Code = code,
                Name = name,
                Description = description,
                IsRequired = isRequired,
                Category = category,
            };
        }

        private static CategoricalClinicalVariable Categorical(
            string name,
            string code,
            string description,
            ClinicalVariableCategory category,
            params string[] acceptedValues)
        {
            return new CategoricalClinicalVariable
            {
                Code = code,
                Name = name,
                Description = description,
                IsRequired = false,
                Category = category,
                AcceptedValues = [.. acceptedValues],
            };
        }

        private static AntibioticGroup Group(string name, Guid? parentId = null)
        {
            return new AntibioticGroup
            {
                Name = name,
                Description = $"{name} antibiotic group",
                ParentId = parentId,
            };
        }

        private static Antibiotic Antibiotic(
            string name,
            Guid groupId,
            AwareClassification classification)
        {
            return new Antibiotic
            {
                Name = name,
                AntibioticGroupId = groupId,
                Classification = classification,
            };
        }

        private static Pathogen Pathogen(string name, bool isAtypical)
        {
            return new Pathogen
            {
                Name = name,
                Description = $"{name} pathogen",
                IsAtypical = isAtypical,
            };
        }

        /*
         * Seeds a realistic diagnosis dataset: clinical variables, a pathogen with a
         * risk factor, an antibiogram, an applied and a retired metric, an applied and a
         * retired treatment protocol, and the antibiotics used by the mocked service.
         */
        private async Task<SeedData> SeedAsync()
        {
            var temperature = Numeric(
                "Body temperature",
                "8310-5",
                "Body temperature measured at the axilla",
                ClinicalVariableCategory.Clinical,
                "Cel",
                NewRange(34.0m, 42.0m, "Cel"),
                NewRange(36.1m, 37.2m, "Cel"));

            var female = Boolean(
                "Female sex",
                "46098-0",
                "Whether the patient is female",
                ClinicalVariableCategory.PersonalInformation,
                isRequired: true);

            var sars = Categorical(
                "SARS-CoV-2 RNA",
                "94500-6",
                "SARS-CoV-2 RNA detected in respiratory specimen",
                ClinicalVariableCategory.Paraclinical,
                "DETECTED",
                "NOT_DETECTED");

            var age = Numeric(
                "Age",
                "30525-0",
                "Age at the time of diagnosis",
                ClinicalVariableCategory.PersonalInformation,
                "a",
                NewRange(0, 120, "a"));

            var criterion = new Criterion
            {
                Name = "Age >= 65 years",
                Formula = new BooleanConstantFormula(true),
            };

            var betaLactams = Group("Beta-lactams");
            var macrolides = Group("Macrolides");
            var carbapenems = Group("Carbapenems", betaLactams.Id);

            var pneumococcus = Pathogen("Streptococcus pneumoniae", isAtypical: false);
            var mycoplasma = Pathogen("Mycoplasma pneumoniae", isAtypical: true);
            var pseudomonas = Pathogen("Pseudomonas aeruginosa", isAtypical: false);

            await _context.ClinicalVariables.AddRangeAsync(
                [temperature, female, sars, age], TestContext.Current.CancellationToken);
            await _context.Criteria.AddAsync(criterion, TestContext.Current.CancellationToken);
            await _context.AntibioticGroups.AddRangeAsync(
                [betaLactams, macrolides, carbapenems], TestContext.Current.CancellationToken);
            await _context.Pathogens.AddRangeAsync(
                [pneumococcus, mycoplasma, pseudomonas], TestContext.Current.CancellationToken);
            await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

            var amoxicillin = Antibiotic("Amoxicillin", betaLactams.Id, AwareClassification.Access);
            var azithromycin = Antibiotic("Azithromycin", macrolides.Id, AwareClassification.Watch);
            var meropenem = Antibiotic("Meropenem", carbapenems.Id, AwareClassification.Reserve);

            await _context.Antibiotics.AddRangeAsync(
                [amoxicillin, azithromycin, meropenem], TestContext.Current.CancellationToken);
            await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

            // CURB-65 is a scoring metric: every criterion contributes 1 point
            var appliedMetric = new ClinicalMetric
            {
                Name = "Severity metrics CURB-65",
                Code = "CURB-65",
                Description = "CURB-65 metrics to assess severity of a patient",
                Version = "2002-2003",
                Publisher = "Hiệp hội Lồng ngực Anh (BTS)",
                IsApplied = true,
            };
            appliedMetric.Rules =
            [
                new ScoringRule
                {
                    ClinicalMetricId = appliedMetric.Id,
                    CriterionId = criterion.Id,
                    ScoreFunction = new NumericConstantFormula(1),
                },
            ];

            var retiredMetric = new ClinicalMetric
            {
                Name = "Pneumonia Severity Index (PSI)",
                Code = "PSI",
                Description = "Retired metric that must not take part in diagnosis",
                Version = "1997",
                Publisher = "PORT",
                IsApplied = false,
            };

            await _context.ClinicalMetrics.AddRangeAsync(
                [appliedMetric, retiredMetric], TestContext.Current.CancellationToken);
            await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

            var appliedProtocol = new TreatmentProtocol
            {
                Severity = Severity.Moderate,
                TreatmentSite = TreatmentSite.Inpatient,
                Version = "2026.1",
                Publisher = "Hiệp hội Lồng ngực Anh (BTS)",
                IsApplied = true,
                Pathogens = [pneumococcus, pseudomonas],
                Criteria = [criterion],
            };
            appliedProtocol.Treatments.Add(new Treatment
            {
                TreatmentProtocolId = appliedProtocol.Id,
                Combination = [amoxicillin, azithromycin],
            });

            var retiredProtocol = new TreatmentProtocol
            {
                Severity = Severity.Severe,
                TreatmentSite = TreatmentSite.IntensiveCareUnit,
                Version = "2025.1",
                Publisher = "Hiệp hội Lồng ngực Anh (BTS)",
                IsApplied = false,
            };

            await _context.TreatmentProtocols.AddRangeAsync(
                [appliedProtocol, retiredProtocol], TestContext.Current.CancellationToken);

            await _context.Antibiograms.AddAsync(new Antibiogram
            {
                MicCategory = MicCategory.Susceptible,
                PathogenId = pneumococcus.Id,
                Antibiotics = [amoxicillin, azithromycin],
            }, TestContext.Current.CancellationToken);

            await _context.SuspectedCauses.AddAsync(new SuspectedCause
            {
                PathogenId = pseudomonas.Id,
                Severity = Severity.Severe,
                TreatmentSite = TreatmentSite.IntensiveCareUnit,
            }, TestContext.Current.CancellationToken);

            await _context.RiskFactors.AddAsync(new RiskFactor
            {
                PathogenId = mycoplasma.Id,
                CriterionId = criterion.Id,
                IsCriticalFactor = true,
            }, TestContext.Current.CancellationToken);

            await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

            return new SeedData(
                temperature,
                female,
                sars,
                age,
                amoxicillin,
                azithromycin,
                meropenem,
                pneumococcus,
                mycoplasma,
                pseudomonas,
                appliedProtocol,
                appliedMetric,
                criterion);
        }

        private static DiagnoseQuery CreateMinimalQuery()
        {
            // Lower boundary: 0 observations, 0 microbial samples, 0 allergic
            // antibiotics, 0 other infections and 0 special medical history entries
            return new DiagnoseQuery
            {
                Observations = [],
                Allergy = new AllergyQuery
                {
                    Type = AllergyType.No,
                    AllergicAntibioticIds = [],
                    OtherAllergy = null,
                },
                SpecialMedicalHistory = [],
                OtherInfections = [],
                MicrobialSamples = [],
            };
        }

        private static DiagnoseQuery CreateFullQuery(SeedData seed)
        {
            return new DiagnoseQuery
            {
                Observations =
                [
                    new ObservationQuery(seed.Temperature.Id, "38.7"),
                    new ObservationQuery(seed.Female.Id, "true"),
                    new ObservationQuery(seed.Sars.Id, "DETECTED"),
                ],
                Allergy = new AllergyQuery
                {
                    Type = AllergyType.Yes,
                    AllergicAntibioticIds = [seed.Amoxicillin.Id],
                    OtherAllergy = null,
                },
                SpecialMedicalHistory =
                [
                    "Chronic kidney disease stage 3a",
                    "Type 2 diabetes mellitus",
                ],
                OtherInfections = [InfectionType.Sepsis, InfectionType.AcuteCystitis],
                MicrobialSamples =
                [
                    new MicrobialSampleQuery
                    {
                        SampleType = MicrobialSampleType.Sputum,
                        MicCategory = MicCategory.Susceptible,
                        PathogenId = seed.Pneumococcus.Id,
                    },
                    new MicrobialSampleQuery
                    {
                        SampleType = MicrobialSampleType.Blood,
                        MicCategory = MicCategory.Resistant,
                        PathogenId = seed.Pseudomonas.Id,
                    },
                ],
            };
        }

        private static DiagnosisModel CreateDiagnosis(
            Severity severity = Severity.Mild,
            TreatmentSite treatmentSite = TreatmentSite.Outpatient)
        {
            return new DiagnosisModel
            {
                Severity = severity,
                TreatmentSite = treatmentSite,
                MetricsSeverityDiagnosis = [],
                HeavySuspected = [],
                WorthSuspected = [],
                Evidences = [],
                MissingVariables = [],
                IsPatientPregnantOrInLactationPhase = false,
                Egfr = null,
            };
        }

        private void SetupValidateSuccess()
        {
            _service
                .Setup(x => x.ValidateObservations(
                    It.IsAny<ClinicalContext>(), It.IsAny<IEnumerable<ClinicalObservation>>()))
                .Returns(Result.Success(ApplicationStatus.Success));
        }

        private void SetupValidateFailure(ApplicationError error)
        {
            _service
                .Setup(x => x.ValidateObservations(
                    It.IsAny<ClinicalContext>(), It.IsAny<IEnumerable<ClinicalObservation>>()))
                .Returns(Result.Failure(error));
        }

        private void SetupDiagnoseSuccess(DiagnosisModel diagnosis)
        {
            _service
                .Setup(x => x.Diagnose(
                    It.IsAny<ClinicalContext>(), It.IsAny<IEnumerable<ClinicalObservation>>()))
                .Returns(Result<DiagnosisModel>.Success(ApplicationStatus.Success, diagnosis));
        }

        private void SetupDiagnoseFailure(ApplicationError error)
        {
            _service
                .Setup(x => x.Diagnose(
                    It.IsAny<ClinicalContext>(), It.IsAny<IEnumerable<ClinicalObservation>>()))
                .Returns(Result<DiagnosisModel>.Failure(error));
        }

        private void SetupMicrobialSuccess(IEnumerable<MicrobialTestResultModel> results)
        {
            _service
                .Setup(x => x.GetMicrobiologicalTestResult(
                    It.IsAny<ClinicalContext>(), It.IsAny<IEnumerable<MicrobialSample>>()))
                .Returns(Result<IEnumerable<MicrobialTestResultModel>>.Success(
                    ApplicationStatus.Success, results));
        }

        private void SetupMicrobialFailure(ApplicationError error)
        {
            _service
                .Setup(x => x.GetMicrobiologicalTestResult(
                    It.IsAny<ClinicalContext>(), It.IsAny<IEnumerable<MicrobialSample>>()))
                .Returns(Result<IEnumerable<MicrobialTestResultModel>>.Failure(error));
        }

        private void SetupTreatmentSuccess(IEnumerable<TreatmentProtocol> protocols)
        {
            _service
                .Setup(x => x.RecommendTreatment(
                    It.IsAny<ClinicalContext>(),
                    It.IsAny<ClinicalPicture>(),
                    It.IsAny<DiagnosisModel>()))
                .Returns(Result<IEnumerable<TreatmentProtocol>>.Success(
                    ApplicationStatus.Success, protocols));
        }

        private void SetupTreatmentFailure(ApplicationError error)
        {
            _service
                .Setup(x => x.RecommendTreatment(
                    It.IsAny<ClinicalContext>(),
                    It.IsAny<ClinicalPicture>(),
                    It.IsAny<DiagnosisModel>()))
                .Returns(Result<IEnumerable<TreatmentProtocol>>.Failure(error));
        }

        #endregion

        #region Happy path

        [Fact]
        public async Task Diagnose_NoObservationNoSampleAtAll_Success()
        {
            /*
             * Lower boundary: a diagnosis request can be made with no observation,
             * no sample, no allergic antibiotic, no other infection and no special
             * medical history. The handler must still run the whole flow and return
             * a well formed empty result.
             */
            SetupValidateSuccess();
            SetupDiagnoseSuccess(CreateDiagnosis());
            SetupMicrobialSuccess([]);
            SetupTreatmentSuccess([]);

            var result = await _handler.HandleAsync(
                CreateMinimalQuery(), TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.NotNull(result.Data);
            Assert.Equal(ApplicationStatus.Success, result.StatusCode);
            Assert.NotEqual(Guid.Empty, result.Data.DiagnosisId);

            Assert.Equal(Severity.Mild, result.Data.Severity);
            Assert.Equal(TreatmentSite.Outpatient, result.Data.TreatmentSite);
            Assert.Empty(result.Data.MetricsSeverityDiagnosis);
            Assert.Empty(result.Data.WorthSuspected);
            Assert.Empty(result.Data.HeavySuspected);
            Assert.Empty(result.Data.Evidences);
            Assert.Empty(result.Data.MissingVariables);
            Assert.False(result.Data.IsPatientPregnantOrInLactationPhase);
            Assert.Null(result.Data.Egfr);
            Assert.Empty(result.Data.OtherInfections);
            Assert.Empty(result.Data.MicrobialTestResults);
            Assert.Equal(AllergyType.No, result.Data.Allergy.Type);
            Assert.Null(result.Data.Allergy.OtherAllergy);
            Assert.Empty(result.Data.Allergy.AllergicAntibiotics);
            Assert.Empty(result.Data.Protocols);
        }

        [Fact]
        public async Task Diagnose_FullClinicalPicture_MapsWholeResult_Success()
        {
            var seed = await SeedAsync();

            ClinicalContext? capturedContext = null;
            List<ClinicalObservation>? capturedObservations = null;
            ClinicalPicture? capturedPicture = null;

            var evidence = new Evidence(
                "CURB-65", new BooleanConstantFormula(true), [], []);
            var diagnosis = new DiagnosisModel
            {
                Severity = Severity.Severe,
                TreatmentSite = TreatmentSite.IntensiveCareUnit,
                MetricsSeverityDiagnosis =
                [
                    new ScoreMetricsSeverityDiagnosis
                    {
                        Code = "CURB-65",
                        Severity = Severity.Severe,
                        TreatmentSite = TreatmentSite.IntensiveCareUnit,
                        Evidences = [evidence],
                        Score = 4,
                    },
                ],
                HeavySuspected = [seed.Pseudomonas],
                WorthSuspected = [seed.Pneumococcus, seed.Mycoplasma],
                Evidences = [evidence],
                MissingVariables = [seed.Age],
                IsPatientPregnantOrInLactationPhase = true,
                Egfr = 48.5,
            };

            var microbialResults = new List<MicrobialTestResultModel>
            {
                new()
                {
                    MicrobialSampleType = MicrobialSampleType.Sputum,
                    Pathogen = seed.Pneumococcus,
                    MicCategory = MicCategory.Susceptible,
                    Antibiotics = [seed.Amoxicillin, seed.Azithromycin],
                },
                new()
                {
                    MicrobialSampleType = MicrobialSampleType.Blood,
                    Pathogen = seed.Pseudomonas,
                    MicCategory = MicCategory.Resistant,
                    Antibiotics = [seed.Meropenem],
                },
            };

            _service
                .Setup(x => x.ValidateObservations(
                    It.IsAny<ClinicalContext>(), It.IsAny<IEnumerable<ClinicalObservation>>()))
                .Callback<ClinicalContext, IEnumerable<ClinicalObservation>>((context, observations) =>
                {
                    capturedContext = context;
                    capturedObservations = [.. observations];
                })
                .Returns(Result.Success(ApplicationStatus.Success));

            _service
                .Setup(x => x.Diagnose(
                    It.IsAny<ClinicalContext>(), It.IsAny<IEnumerable<ClinicalObservation>>()))
                .Returns(Result<DiagnosisModel>.Success(ApplicationStatus.Success, diagnosis));

            _service
                .Setup(x => x.GetMicrobiologicalTestResult(
                    It.IsAny<ClinicalContext>(), It.IsAny<IEnumerable<MicrobialSample>>()))
                .Returns(Result<IEnumerable<MicrobialTestResultModel>>.Success(
                    ApplicationStatus.Success, microbialResults));

            _service
                .Setup(x => x.RecommendTreatment(
                    It.IsAny<ClinicalContext>(),
                    It.IsAny<ClinicalPicture>(),
                    It.IsAny<DiagnosisModel>()))
                .Callback<ClinicalContext, ClinicalPicture, DiagnosisModel>((_, picture, _) =>
                    capturedPicture = picture)
                .Returns(Result<IEnumerable<TreatmentProtocol>>.Success(
                    ApplicationStatus.Success, [seed.AppliedProtocol]));

            var query = CreateFullQuery(seed);

            var result = await _handler.HandleAsync(
                query, TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.NotNull(result.Data);
            Assert.Equal(ApplicationStatus.Success, result.StatusCode);
            Assert.NotEqual(Guid.Empty, result.Data.DiagnosisId);

            // Diagnosis fields are carried over unchanged
            Assert.Equal(Severity.Severe, result.Data.Severity);
            Assert.Equal(TreatmentSite.IntensiveCareUnit, result.Data.TreatmentSite);

            var metricDiagnosis = Assert.Single(result.Data.MetricsSeverityDiagnosis);
            Assert.Equal("CURB-65", metricDiagnosis.Code);
            Assert.Equal(Severity.Severe, metricDiagnosis.Severity);
            Assert.Equal(TreatmentSite.IntensiveCareUnit, metricDiagnosis.TreatmentSite);

            var heavy = Assert.Single(result.Data.HeavySuspected);
            Assert.Equal(seed.Pseudomonas.Id, heavy.Id);
            Assert.Equal("Pseudomonas aeruginosa", heavy.Name);

            Assert.Equal(
                [seed.Pneumococcus.Id, seed.Mycoplasma.Id],
                result.Data.WorthSuspected.Select(x => x.Id));

            var resultEvidence = Assert.Single(result.Data.Evidences);
            Assert.Equal("CURB-65", resultEvidence.Source);

            var missing = Assert.Single(result.Data.MissingVariables);
            Assert.Equal(seed.Age.Id, missing.Id);
            Assert.Equal(seed.Age.Name, missing.Name);
            Assert.Equal(seed.Age.Code, missing.Code);

            Assert.True(result.Data.IsPatientPregnantOrInLactationPhase);
            Assert.Equal(48.5, result.Data.Egfr);

            // Other infections come from the request, not from the service
            Assert.Equal(
                [InfectionType.Sepsis, InfectionType.AcuteCystitis],
                result.Data.OtherInfections);

            // Allergy is echoed with the resolved antibiotic details
            Assert.Equal(AllergyType.Yes, result.Data.Allergy.Type);
            Assert.Null(result.Data.Allergy.OtherAllergy);
            var allergic = Assert.Single(result.Data.Allergy.AllergicAntibiotics);
            Assert.Equal(seed.Amoxicillin.Id, allergic.Id);
            Assert.Equal("Amoxicillin", allergic.Name);
            Assert.Equal(AwareClassification.Access, allergic.Classification);

            // Microbial test results keep sample type, MIC category, pathogen and medicine list
            var microbial = result.Data.MicrobialTestResults.ToList();
            Assert.Equal(2, microbial.Count);

            var sputum = Assert.Single(microbial, x => x.SampleType == MicrobialSampleType.Sputum);
            Assert.Equal(MicCategory.Susceptible, sputum.Category);
            Assert.Equal(seed.Pneumococcus.Id, sputum.Pathogen.Id);
            Assert.Equal(
                [seed.Amoxicillin.Id, seed.Azithromycin.Id],
                sputum.Antibiotics.Select(x => x.Id));
            Assert.Equal(
                [AwareClassification.Access, AwareClassification.Watch],
                sputum.Antibiotics.Select(x => x.Classification));

            var blood = Assert.Single(microbial, x => x.SampleType == MicrobialSampleType.Blood);
            Assert.Equal(MicCategory.Resistant, blood.Category);
            Assert.Equal(seed.Pseudomonas.Id, blood.Pathogen.Id);
            Assert.Equal([seed.Meropenem.Id], blood.Antibiotics.Select(x => x.Id));

            // Treatment protocol mapping keeps identity, classification and medicine combinations
            var protocol = Assert.Single(result.Data.Protocols);
            Assert.Equal(seed.AppliedProtocol.Id, protocol.Id);
            Assert.Equal(Severity.Moderate, protocol.Severity);
            Assert.Equal(TreatmentSite.Inpatient, protocol.TreatmentSite);
            Assert.Equal(
                [seed.Pneumococcus.Id, seed.Pseudomonas.Id],
                protocol.Pathogens.Select(x => x.Id));

            var criterion = Assert.Single(protocol.Criteria);
            Assert.Equal(seed.ProtocolCriterion.Id, criterion.Id);
            Assert.Equal(seed.ProtocolCriterion.Name, criterion.Name);
            Assert.Equal(seed.ProtocolCriterion.Formula.ToString(), criterion.Formula);

            var composition = Assert.Single(protocol.Medicines);
            Assert.Equal(
                [seed.Amoxicillin.Id, seed.Azithromycin.Id],
                composition.Select(x => x.Id));

            // ----- captured clinical context: the handler loaded what the service needs -----
            Assert.NotNull(capturedContext);
            Assert.Equal(4, capturedContext.Variables.Count());
            // Only applied metrics take part in the diagnosis
            _ = Assert.Single(capturedContext.Metrics);
            Assert.Equal(seed.AppliedMetric.Id, capturedContext.Metrics.Single().Id);

            Assert.Equal(3, capturedContext.Pathogens.Count());
            var mycoplasma = Assert.Single(
                capturedContext.Pathogens, x => x.Id == seed.Mycoplasma.Id);
            var riskFactor = Assert.Single(mycoplasma.RiskFactors);
            Assert.True(riskFactor.IsCriticalFactor);
            Assert.Equal(seed.ProtocolCriterion.Id, riskFactor.Criterion.Id);

            var suspectedCause = Assert.Single(capturedContext.SuspectedCauses);
            Assert.Equal(seed.Pseudomonas.Id, suspectedCause.Pathogen.Id);
            Assert.Equal(Severity.Severe, suspectedCause.Severity);

            Assert.Equal(3, capturedContext.Antibiotics.Count());
            var meropenem = Assert.Single(
                capturedContext.Antibiotics, x => x.Id == seed.Meropenem.Id);
            Assert.Equal("Carbapenems", meropenem.AntibioticGroup.Name);

            var carbapenems = Assert.Single(
                capturedContext.AntibioticGroups, x => x.Name == "Carbapenems");
            Assert.NotNull(carbapenems.Parent);
            Assert.Equal("Beta-lactams", carbapenems.Parent.Name);

            var antibiogram = Assert.Single(capturedContext.Antibiograms);
            Assert.Equal(seed.Pneumococcus.Id, antibiogram.Pathogen.Id);
            Assert.Equal(2, antibiogram.Antibiotics.Count);

            // Only applied treatment protocols take part in the diagnosis
            var appliedProtocol = Assert.Single(capturedContext.TreatmentProtocols);
            Assert.Equal(seed.AppliedProtocol.Id, appliedProtocol.Id);
            Assert.Equal(2, appliedProtocol.Pathogens.Count);
            Assert.Single(appliedProtocol.Criteria);
            var treatment = Assert.Single(appliedProtocol.Treatments);
            Assert.Equal(2, treatment.Combination.Count);
            Assert.All(treatment.Combination, x => Assert.NotNull(x.AntibioticGroup));

            // ----- captured observations: values were parsed according to the variable type -----
            Assert.NotNull(capturedObservations);
            Assert.Equal(3, capturedObservations.Count);

            var temperature = Assert.Single(
                capturedObservations, x => x.Variable.Id == seed.Temperature.Id);
            Assert.Equal(38.7m, temperature.NumericValue);
            Assert.Null(temperature.BooleanValue);
            Assert.Null(temperature.CategoricalValue);

            var female = Assert.Single(
                capturedObservations, x => x.Variable.Id == seed.Female.Id);
            Assert.True(female.BooleanValue);
            Assert.Null(female.NumericValue);
            Assert.Null(female.CategoricalValue);

            var sars = Assert.Single(
                capturedObservations, x => x.Variable.Id == seed.Sars.Id);
            Assert.Equal("DETECTED", sars.CategoricalValue);
            Assert.Null(sars.NumericValue);
            Assert.Null(sars.BooleanValue);

            // ----- captured clinical picture: the request was resolved against the context -----
            Assert.NotNull(capturedPicture);
            Assert.Equal(3, capturedPicture.Observations.Count());
            Assert.Equal(AllergyType.Yes, capturedPicture.Allergy.Type);
            Assert.Null(capturedPicture.Allergy.OtherAllergy);
            var pictureAllergic = Assert.Single(capturedPicture.Allergy.AllergicAntibiotics);
            Assert.Equal(seed.Amoxicillin.Id, pictureAllergic.Id);

            Assert.Equal(
                ["Chronic kidney disease stage 3a", "Type 2 diabetes mellitus"],
                capturedPicture.SpecialMedicalHistory);
            Assert.Equal(
                [InfectionType.Sepsis, InfectionType.AcuteCystitis],
                capturedPicture.OtherInfections);

            var pictureSamples = capturedPicture.MicrobialSamples.ToList();
            Assert.Equal(2, pictureSamples.Count);
            Assert.Equal(
                [MicrobialSampleType.Sputum, MicrobialSampleType.Blood],
                pictureSamples.Select(x => x.SampleType));
            Assert.Equal(
                [seed.Pneumococcus.Id, seed.Pseudomonas.Id],
                pictureSamples.Select(x => x.Pathogen.Id));
        }

        [Fact]
        public async Task Diagnose_NonRegisteredAntibioticAllergy_MapsOtherAllergy_Success()
        {
            SetupValidateSuccess();
            SetupDiagnoseSuccess(CreateDiagnosis());
            SetupMicrobialSuccess([]);
            SetupTreatmentSuccess([]);

            var query = CreateMinimalQuery() with
            {
                Allergy = new AllergyQuery
                {
                    Type = AllergyType.NonRegisteredAntibiotic,
                    AllergicAntibioticIds = [],
                    OtherAllergy = "Penicillin G",
                },
            };

            var result = await _handler.HandleAsync(
                query, TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.NotNull(result.Data);

            Assert.Equal(AllergyType.NonRegisteredAntibiotic, result.Data.Allergy.Type);
            Assert.Equal("Penicillin G", result.Data.Allergy.OtherAllergy);
            Assert.Empty(result.Data.Allergy.AllergicAntibiotics);
        }

        // Numeric boundary values of the accepted body temperature range: 34.0 and 42.0
        [Theory]
        [InlineData("34.0")]
        [InlineData("38.75")]
        [InlineData("42.0")]
        public async Task Diagnose_NumericObservation_ParsesInvariantDecimal(string value)
        {
            var seed = await SeedAsync();

            ClinicalObservation? captured = null;
            _service
                .Setup(x => x.ValidateObservations(
                    It.IsAny<ClinicalContext>(), It.IsAny<IEnumerable<ClinicalObservation>>()))
                .Callback<ClinicalContext, IEnumerable<ClinicalObservation>>((_, observations) =>
                    captured = observations.Single())
                .Returns(Result.Success(ApplicationStatus.Success));
            SetupDiagnoseSuccess(CreateDiagnosis());
            SetupMicrobialSuccess([]);
            SetupTreatmentSuccess([]);

            var query = CreateMinimalQuery() with
            {
                Observations = [new ObservationQuery(seed.Temperature.Id, value)],
            };

            var result = await _handler.HandleAsync(
                query, TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.NotNull(captured);
            Assert.Equal(
                decimal.Parse(value, CultureInfo.InvariantCulture), captured.NumericValue);
            Assert.Null(captured.BooleanValue);
            Assert.Null(captured.CategoricalValue);
        }

        #endregion

        #region Fail path

        [Fact]
        public async Task Diagnose_ObservationVariableNotFound_Fails()
        {
            await SeedAsync();

            var query = CreateMinimalQuery() with
            {
                Observations = [new ObservationQuery(Guid.CreateVersion7(), "38.7")],
            };

            var result = await _handler.HandleAsync(
                query, TestContext.Current.CancellationToken);

            Assert.True(result.IsFailure());
            Assert.Null(result.Data);
            Assert.Equal(ApplicationStatus.BadRequest, result.StatusCode);
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.BadRequest, result.Error.StatusCode);

            _service.Verify(x => x.ValidateObservations(
                It.IsAny<ClinicalContext>(), It.IsAny<IEnumerable<ClinicalObservation>>()),
                Times.Never);
        }

        [Fact]
        public async Task Diagnose_BooleanObservationWithNonBooleanValue_Fails()
        {
            var seed = await SeedAsync();

            var query = CreateMinimalQuery() with
            {
                Observations = [new ObservationQuery(seed.Female.Id, "yes")],
            };

            var result = await _handler.HandleAsync(
                query, TestContext.Current.CancellationToken);

            Assert.True(result.IsFailure());
            Assert.Null(result.Data);
            Assert.Equal(ApplicationStatus.BadRequest, result.StatusCode);
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.BadRequest, result.Error.StatusCode);

            _service.Verify(x => x.ValidateObservations(
                It.IsAny<ClinicalContext>(), It.IsAny<IEnumerable<ClinicalObservation>>()),
                Times.Never);
        }

        [Fact]
        public async Task Diagnose_NumericObservationWithNonNumericValue_Fails()
        {
            var seed = await SeedAsync();

            var query = CreateMinimalQuery() with
            {
                Observations = [new ObservationQuery(seed.Temperature.Id, "fever")],
            };

            var result = await _handler.HandleAsync(
                query, TestContext.Current.CancellationToken);

            Assert.True(result.IsFailure());
            Assert.Null(result.Data);
            Assert.Equal(ApplicationStatus.BadRequest, result.StatusCode);
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.BadRequest, result.Error.StatusCode);

            _service.Verify(x => x.ValidateObservations(
                It.IsAny<ClinicalContext>(), It.IsAny<IEnumerable<ClinicalObservation>>()),
                Times.Never);
        }

        // Boundary: empty and whitespace are the only categorical values rejected here
        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public async Task Diagnose_CategoricalObservationWithBlankValue_Fails(string value)
        {
            var seed = await SeedAsync();

            var query = CreateMinimalQuery() with
            {
                Observations = [new ObservationQuery(seed.Sars.Id, value)],
            };

            var result = await _handler.HandleAsync(
                query, TestContext.Current.CancellationToken);

            Assert.True(result.IsFailure());
            Assert.Null(result.Data);
            Assert.Equal(ApplicationStatus.BadRequest, result.StatusCode);
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.BadRequest, result.Error.StatusCode);

            _service.Verify(x => x.ValidateObservations(
                It.IsAny<ClinicalContext>(), It.IsAny<IEnumerable<ClinicalObservation>>()),
                Times.Never);
        }

        [Fact]
        public async Task Diagnose_MicrobialSampleWithUnknownPathogen_Fails()
        {
            var seed = await SeedAsync();

            var query = CreateMinimalQuery() with
            {
                MicrobialSamples =
                [
                    new MicrobialSampleQuery
                    {
                        SampleType = MicrobialSampleType.Sputum,
                        MicCategory = MicCategory.Susceptible,
                        PathogenId = seed.Pneumococcus.Id,
                    },
                    new MicrobialSampleQuery
                    {
                        SampleType = MicrobialSampleType.Blood,
                        MicCategory = MicCategory.Resistant,
                        PathogenId = Guid.CreateVersion7(),
                    },
                ],
            };

            var result = await _handler.HandleAsync(
                query, TestContext.Current.CancellationToken);

            Assert.True(result.IsFailure());
            Assert.Null(result.Data);
            Assert.Equal(ApplicationStatus.BadRequest, result.StatusCode);
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.BadRequest, result.Error.StatusCode);

            _service.Verify(x => x.ValidateObservations(
                It.IsAny<ClinicalContext>(), It.IsAny<IEnumerable<ClinicalObservation>>()),
                Times.Never);
        }

        [Fact]
        public async Task Diagnose_AllergyWithUnknownAntibiotic_Fails()
        {
            await SeedAsync();

            var query = CreateMinimalQuery() with
            {
                Allergy = new AllergyQuery
                {
                    Type = AllergyType.Yes,
                    AllergicAntibioticIds = [Guid.CreateVersion7()],
                },
            };

            var result = await _handler.HandleAsync(
                query, TestContext.Current.CancellationToken);

            Assert.True(result.IsFailure());
            Assert.Null(result.Data);
            Assert.Equal(ApplicationStatus.BadRequest, result.StatusCode);
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.BadRequest, result.Error.StatusCode);

            _service.Verify(x => x.ValidateObservations(
                It.IsAny<ClinicalContext>(), It.IsAny<IEnumerable<ClinicalObservation>>()),
                Times.Never);
        }

        [Fact]
        public async Task Diagnose_ObservationValidationFails_ReturnsServiceError()
        {
            var error = new ApplicationError(
                ApplicationStatus.BusinessRuleViolation, "Observation is not clinically valid");
            SetupValidateFailure(error);

            var result = await _handler.HandleAsync(
                CreateMinimalQuery(), TestContext.Current.CancellationToken);

            Assert.True(result.IsFailure());
            Assert.Null(result.Data);
            Assert.Equal(ApplicationStatus.BusinessRuleViolation, result.StatusCode);
            Assert.Same(error, result.Error);

            _service.Verify(x => x.Diagnose(
                It.IsAny<ClinicalContext>(), It.IsAny<IEnumerable<ClinicalObservation>>()),
                Times.Never);
        }

        [Fact]
        public async Task Diagnose_SeverityAndInfectionDiagnosisFails_ReturnsServiceError()
        {
            var error = new ApplicationError(
                ApplicationStatus.ServerError, "Severity and infection diagnosis failed");
            SetupValidateSuccess();
            SetupDiagnoseFailure(error);

            var result = await _handler.HandleAsync(
                CreateMinimalQuery(), TestContext.Current.CancellationToken);

            Assert.True(result.IsFailure());
            Assert.Null(result.Data);
            Assert.Equal(ApplicationStatus.ServerError, result.StatusCode);
            Assert.Same(error, result.Error);

            _service.Verify(x => x.GetMicrobiologicalTestResult(
                It.IsAny<ClinicalContext>(), It.IsAny<IEnumerable<MicrobialSample>>()),
                Times.Never);
        }

        [Fact]
        public async Task Diagnose_MicrobialTestFails_ReturnsServiceError()
        {
            var error = new ApplicationError(
                ApplicationStatus.ThirdPartyServiceFailure, "Microbial test failed");
            SetupValidateSuccess();
            SetupDiagnoseSuccess(CreateDiagnosis());
            SetupMicrobialFailure(error);

            var result = await _handler.HandleAsync(
                CreateMinimalQuery(), TestContext.Current.CancellationToken);

            Assert.True(result.IsFailure());
            Assert.Null(result.Data);
            Assert.Equal(ApplicationStatus.ThirdPartyServiceFailure, result.StatusCode);
            Assert.Same(error, result.Error);

            _service.Verify(x => x.RecommendTreatment(
                It.IsAny<ClinicalContext>(),
                It.IsAny<ClinicalPicture>(),
                It.IsAny<DiagnosisModel>()),
                Times.Never);
        }

        [Fact]
        public async Task Diagnose_RecommendTreatmentFails_ReturnsServiceError()
        {
            var error = new ApplicationError(
                ApplicationStatus.BusinessRuleViolation, "No treatment protocol matched");
            SetupValidateSuccess();
            SetupDiagnoseSuccess(CreateDiagnosis());
            SetupMicrobialSuccess([]);
            SetupTreatmentFailure(error);

            var result = await _handler.HandleAsync(
                CreateMinimalQuery(), TestContext.Current.CancellationToken);

            Assert.True(result.IsFailure());
            Assert.Null(result.Data);
            Assert.Equal(ApplicationStatus.BusinessRuleViolation, result.StatusCode);
            Assert.Same(error, result.Error);
        }

        #endregion
    }
}
