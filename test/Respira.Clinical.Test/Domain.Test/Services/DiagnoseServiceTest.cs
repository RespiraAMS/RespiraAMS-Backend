using Microsoft.Extensions.Logging;
using Moq;
using Respira.Clinical.Domain.Entities;
using Respira.Clinical.Domain.Enums;
using Respira.Clinical.Domain.Models;
using Respira.Clinical.Domain.Services;
using Respira.Clinical.Infrastructure.Data;
using Respira.ServiceDefaults.Contracts.Results;
using Xunit;

namespace Respira.Clinical.Domain.Test.Services
{
    /// <summary>
    /// This class tests individual methods of the <see cref="DiagnoseService"/> class.
    /// </summary>
    public class DiagnoseServiceTest
    {
        private readonly DiagnoseService _service;
        private static readonly ClinicalContext _context = CreateContext();

        public DiagnoseServiceTest()
        {
            var logger = new Mock<ILogger<DiagnoseService>>().Object;
            _service = new DiagnoseService(logger);
        }

        // Initialize clinical context
        // To avoid duplicate code, we will use the seed data logic from Infrastructure
        // to seed the clinical context
        private static ClinicalContext CreateContext()
        {
            var seedData = DataSeeder.Load("Data/context.jsonc");
            return new ClinicalContext
            {
                Antibiotics = seedData.Antibiotics,
                Metrics = seedData.ClinicalMetrics,
                Pathogens = seedData.Pathogens,
                SuspectedCauses = seedData.SuspectedCauses,
                AntibioticGroups = seedData.AntibioticGroups,
                Variables = seedData.ClinicalVariables,
                TreatmentProtocols = seedData.Treatments,
                Antibiograms = seedData.Antibiograms
            };
        }

        private static ClinicalObservation Obs(string code, decimal value)
        {
            var variable = _context.Variables.FirstOrDefault(x => x.Code.Equals(code)) ?? throw new ArgumentException($"Variable {code} not found");
            return new ClinicalObservation(variable, value);
        }

        private static ClinicalObservation Obs(string code, bool value = true)
        {
            var variable = _context.Variables.FirstOrDefault(x => x.Code.Equals(code)) ?? throw new ArgumentException($"Variable {code} not found");
            return new ClinicalObservation(variable, value);
        }

        private static ClinicalObservation Obs(string code, string value)
        {
            var variable = _context.Variables.FirstOrDefault(x => x.Code.Equals(code)) ?? throw new ArgumentException($"Variable {code} not found");
            return new ClinicalObservation(variable, value);
        }

        private static Pathogen Pathogen(string Name)
        {
            return _context.Pathogens.FirstOrDefault(x => x.Name.Equals(Name)) ?? throw new ArgumentException($"Pathogen {Name} not found");
        }

        private static AntibioticGroup AntibioticGroup(string Name)
        {
            return _context.AntibioticGroups.FirstOrDefault(x => x.Name.Equals(Name)) ?? throw new ArgumentException($"Antibiotic group {Name} not found");
        }

        private static Antibiotic Antibiotic(string Name)
        {
            return _context.Antibiotics.FirstOrDefault(x => x.Name.Equals(Name)) ?? throw new ArgumentException($"Antibiotic {Name} not found");
        }

        private static ClinicalVariable Variable(string code)
        {
            return _context.Variables.FirstOrDefault(x => x.Code.Equals(code)) ?? throw new ArgumentException($"Variable {code} not found");
        }

        // Realistic values for every required numeric variable, used to build a baseline of valid
        // observations. Each test only overrides/removes the variable it is interested in.
        private static readonly IReadOnlyDictionary<string, decimal> RequiredNumericValues = new Dictionary<string, decimal>
        {
            ["AGE"] = 72,            // year
            ["WEIGHT"] = 68.5m,      // kg
            ["HEIGHT"] = 170,        // cm
            ["PULSE"] = 88,          // bpm
            ["SBP"] = 118,           // mmHg
            ["DBP"] = 74,            // mmHg
            ["TEMPERATURE"] = 37.2m, // Celsius
            ["RR"] = 18,             // breaths/min
            ["FIO2"] = 21,           // %
            ["SPO2"] = 97,           // %
        };

        private static List<ClinicalObservation> ValidRequiredObservations()
        {
            return [.. _context.Variables
                .Where(v => v.IsRequired)
                .Select(v => v.ValueType switch
                {
                    ClinicalValueType.Numeric => Obs(v.Code, RequiredNumericValues[v.Code]),
                    ClinicalValueType.Boolean => Obs(v.Code, false),
                    ClinicalValueType.Categorical => Obs(v.Code, "MALE"),
                    _ => throw new ArgumentException($"Unsupported value type for variable {v.Code}")
                })];
        }

        private static List<ClinicalObservation> WithObservation(IEnumerable<ClinicalObservation> observations, ClinicalObservation replacement)
        {
            return [.. observations.Where(o => !o.Variable.Code.Equals(replacement.Variable.Code)), replacement];
        }

        private static List<ClinicalObservation> WithoutVariable(IEnumerable<ClinicalObservation> observations, string code)
        {
            return [.. observations.Where(o => !o.Variable.Code.Equals(code))];
        }

        // Every per-variable validation error must be reported as a variable id -> message map,
        // so the client knows exactly which observation is wrong.
        private static Dictionary<Guid, string> AssertInvalidObservationsFailure(Result result)
        {
            Assert.True(result.IsFailure());
            Assert.Equal(ApplicationStatus.BadRequest, result.StatusCode);
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.BadRequest, result.Error.StatusCode);
            return Assert.IsType<Dictionary<Guid, string>>(result.Error.Detail);
        }

        private static ClinicalMetric Metric(string code)
        {
            return _context.Metrics.FirstOrDefault(x => x.Code.Equals(code)) ?? throw new ArgumentException($"Metric {code} not found");
        }

        // Build a context with only the given metrics applied, reusing the seed data for
        // everything else (variables, pathogens, suspected causes,...)
        private static ClinicalContext ContextWithMetrics(params ClinicalMetric[] metrics)
        {
            return new ClinicalContext
            {
                Variables = _context.Variables,
                Metrics = metrics,
                Pathogens = _context.Pathogens,
                SuspectedCauses = _context.SuspectedCauses,
                AntibioticGroups = _context.AntibioticGroups,
                Antibiotics = _context.Antibiotics,
                Antibiograms = _context.Antibiograms,
                TreatmentProtocols = _context.TreatmentProtocols,
            };
        }

        private static ClinicalMetric UnsupportedMetric()
        {
            return new ClinicalMetric
            {
                Name = "Unsupported metric",
                Code = "UNSUPPORTED-METRIC",
                Description = "Metric that the diagnose engine does not know",
                Version = "1.0.0",
                Publisher = "Respira test",
                IsApplied = true,
                Rules = [],
            };
        }

        private static HashSet<Guid> SuspectedPathogenIds(Severity severity, TreatmentSite treatmentSite)
        {
            return [.. _context.SuspectedCauses
                .Where(sc => sc.Severity == severity && sc.TreatmentSite == treatmentSite)
                .Select(sc => sc.PathogenId)];
        }

        // Build an antibiogram for a pathogen and MIC category, using real antibiotics
        // from the seed data so the returned susceptibility profile is meaningful
        private static Antibiogram Antibiogram(string pathogenName, MicCategory micCategory, params string[] antibioticNames)
        {
            var pathogen = Pathogen(pathogenName);
            return new Antibiogram
            {
                PathogenId = pathogen.Id,
                Pathogen = pathogen,
                MicCategory = micCategory,
                Antibiotics = [.. antibioticNames.Select(Antibiotic)],
            };
        }

        private static MicrobialSample Sample(string pathogenName, MicCategory micCategory, MicrobialSampleType sampleType = MicrobialSampleType.Sputum)
        {
            return new MicrobialSample
            {
                SampleType = sampleType,
                MicCategory = micCategory,
                Pathogen = Pathogen(pathogenName),
            };
        }

        // Build a context with the given antibiograms, reusing the seed data for
        // everything else
        private static ClinicalContext ContextWithAntibiograms(params Antibiogram[] antibiograms)
        {
            return new ClinicalContext
            {
                Variables = _context.Variables,
                Metrics = _context.Metrics,
                Pathogens = _context.Pathogens,
                SuspectedCauses = _context.SuspectedCauses,
                AntibioticGroups = _context.AntibioticGroups,
                Antibiotics = _context.Antibiotics,
                Antibiograms = antibiograms,
                TreatmentProtocols = _context.TreatmentProtocols,
            };
        }

        // Build a treatment protocol for recommendation tests. Each combination is one
        // alternative treatment (a list of antibiotics used together).
        private static TreatmentProtocol Protocol(
            Severity severity,
            TreatmentSite treatmentSite,
            Pathogen[] pathogens,
            Criterion[] criteria,
            params string[][] combinations)
        {
            var protocol = new TreatmentProtocol
            {
                Severity = severity,
                TreatmentSite = treatmentSite,
                Version = "1.0.0",
                Publisher = "Respira test",
                IsApplied = true,
                Pathogens = [.. pathogens],
                Criteria = [.. criteria],
            };
            protocol.Treatments = [.. combinations.Select(combination => new Treatment
            {
                TreatmentProtocolId = protocol.Id,
                TreatmentProtocol = protocol,
                Combination = [.. combination.Select(Antibiotic)],
            })];
            return protocol;
        }

        // Same as Protocol(...) but marked as not applied, so the clinical context
        // filter must drop it before any recommendation
        private static TreatmentProtocol NonAppliedProtocol(
            Severity severity,
            TreatmentSite treatmentSite,
            params string[][] combinations)
        {
            var protocol = Protocol(severity, treatmentSite, [], [], combinations);
            protocol.IsApplied = false;
            return protocol;
        }

        private static Criterion AgeAtLeastCriterion(int age)
        {
            return new Criterion
            {
                Name = $"Tuổi >= {age}",
                Formula = new BinaryFormula(
                    new VariableFormula(Variable("AGE")),
                    new NumericConstantFormula(age),
                    ExpressionOperator.Gte),
            };
        }

        private static Criterion BooleanCriterion(string variableCode, bool expected)
        {
            return new Criterion
            {
                Name = $"{variableCode} = {expected}",
                Formula = new BinaryFormula(
                    new VariableFormula(Variable(variableCode)),
                    new BooleanConstantFormula(expected),
                    ExpressionOperator.Eq),
            };
        }

        // Build the minimum diagnosis required by the treatment recommendation
        private static Diagnosis DiagnosisFor(Severity severity, TreatmentSite treatmentSite, params Pathogen[] heavySuspected)
        {
            return new Diagnosis
            {
                Severity = severity,
                TreatmentSite = treatmentSite,
                MetricsSeverityDiagnosis = [],
                HeavySuspected = heavySuspected,
                WorthSuspected = [],
                Evidences = [],
                MissingVariables = [],
                IsPatientPregnantOrInLactationPhase = false,
            };
        }

        private static ClinicalPicture Picture(IEnumerable<ClinicalObservation> observations, params Antibiotic[] allergicAntibiotics)
        {
            return new ClinicalPicture
            {
                Observations = observations,
                Allergy = new Allergy
                {
                    Type = allergicAntibiotics.Length == 0 ? AllergyType.No : AllergyType.Yes,
                    AllergicAntibiotics = allergicAntibiotics,
                },
            };
        }

        // Build a context with the given treatment protocols, reusing the seed data
        // for everything else
        private static ClinicalContext ContextWithProtocols(params TreatmentProtocol[] protocols)
        {
            return new ClinicalContext
            {
                Variables = _context.Variables,
                Metrics = _context.Metrics,
                Pathogens = _context.Pathogens,
                SuspectedCauses = _context.SuspectedCauses,
                AntibioticGroups = _context.AntibioticGroups,
                Antibiotics = _context.Antibiotics,
                Antibiograms = _context.Antibiograms,
                TreatmentProtocols = protocols,
            };
        }

        #region Test eGFR

        [Theory]
        [InlineData(60, 62d, true, true, 103.2d)]
        [InlineData(30, 115d, true, false, 48.1d)]
        [InlineData(17, 89d, false, true, 118.4d)]
        [InlineData(80, 200d, false, false, 28.0d)]
        public void CalculateEgfr_Success(
            int age,
            double scr,
            bool isFemale,
            bool isBlackAmerican,
            double expected)
        {
            // Allow for 1 floating point precision error 
            Assert.Equal(expected, _service.CalculateEgfr(age, scr, isFemale, isBlackAmerican), 1);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(-100)]
        public void CalculateEgfr_InvalidAge_Throws(int age)
        {
            Assert.Throws<ArgumentException>(() => _service.CalculateEgfr(age, 62, true, true));
        }

        [Theory]
        [InlineData(0d)]
        [InlineData(-1d)]
        [InlineData(-10.5d)]
        public void CalculateEgfr_InvalidScr_Throws(double scr)
        {
            Assert.Throws<ArgumentException>(() => _service.CalculateEgfr(65, scr, true, true));
        }

        #endregion

        #region Test CalculateMetricsScore

        public static readonly TheoryData<string, List<ClinicalObservation>, decimal> ScoresCase = [
            ("CURB-65", [
                Obs("AGE", 65),
                Obs("UREA", 0),
                Obs("SBP", 120)
            ], 1),
            ("CURB-65", [
                Obs("AGE", 64),
                Obs("UREA", 7),
                Obs("SBP", 90),
                Obs("GENDER", "MALE") // Unrelated variable should not affect the score
            ], 0),
            ("PSI", [
                Obs("AGE", 65),
                Obs("GENDER", "FEMALE"),
                Obs("CHRONIC-RENAL")
            ], 65)
        ];

        [Theory]
        [MemberData(nameof(ScoresCase))]
        public void CalculateMetricsScore_Success(string metricCode, List<ClinicalObservation> observations, decimal expected)
        {
            // Get metrics
            var metric = _context.Metrics.FirstOrDefault(x => x.Code.Equals(metricCode)) ?? throw new ArgumentException($"Metric {metricCode} not found");
            var (score, evidences) = _service.CalculateMetricsScore(metric, observations);
            Assert.Equal(score, expected);
            Assert.NotEmpty(evidences);

            // The total evidence generated must equal the number of rule in a metric,
            // whether satisfied or not
            Assert.Equal(metric.Rules.Count, evidences.Count);
        }

        [Fact]
        public void CalculateMetricsScore_MajorMinorMetric_Throw()
        {
            var metric = _context.Metrics.FirstOrDefault(x => x.Code.Equals("IDSA/ATS")) ?? throw new Exception("IDSA/ATS metric not found");
            Assert.Throws<ArgumentException>(() => _service.CalculateMetricsScore(metric, []));
        }

        #endregion

        #region Test Curb65

        [Theory]
        [InlineData(0, Severity.Mild, TreatmentSite.Outpatient)]
        [InlineData(1, Severity.Mild, TreatmentSite.Outpatient)]
        [InlineData(2, Severity.Moderate, TreatmentSite.Inpatient)]
        [InlineData(3, Severity.Severe, TreatmentSite.Inpatient)]
        [InlineData(4, Severity.Severe, TreatmentSite.IntensiveCareUnit)]
        [InlineData(5, Severity.Severe, TreatmentSite.IntensiveCareUnit)]
        public void Curb65_Success(int score, Severity severity, TreatmentSite treatmentSite)
        {
            var result = _service.Curb65(score, []);
            Assert.Equal(severity, result.Severity);
            Assert.Equal(treatmentSite, result.TreatmentSite);
        }

        [Theory]
        [InlineData(0, Severity.Mild, TreatmentSite.Outpatient)]
        [InlineData(1, Severity.Moderate, TreatmentSite.Inpatient)]
        [InlineData(2, Severity.Moderate, TreatmentSite.Inpatient)]
        [InlineData(3, Severity.Severe, TreatmentSite.Inpatient)]
        [InlineData(4, Severity.Severe, TreatmentSite.IntensiveCareUnit)]
        public void Crb65_Success(int score, Severity severity, TreatmentSite treatmentSite)
        {
            var result = _service.Crb65(score, []);
            Assert.Equal(severity, result.Severity);
            Assert.Equal(treatmentSite, result.TreatmentSite);
        }

        [Theory]
        [InlineData(-1)] // Boundary value
        [InlineData(-10)]
        public void Curb65_InvalidScore_Negative_Throw(int score)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => _service.Curb65(score, []));
        }

        [Theory]
        [InlineData(-1)] // Boundary value
        [InlineData(-10)]
        public void Crb65_InvalidScore_Negative_Throw(int score)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => _service.Crb65(score, []));
        }


        [Theory]
        [InlineData(6)] // Boundary value
        [InlineData(200)]
        public void Curb65_InvalidScore_UnexpectedValue_Throw(int score)
        {
            Assert.Throws<InvalidOperationException>(() => _service.Curb65(score, []));
        }

        [Theory]
        [InlineData(5)] // Boundary value
        [InlineData(100)]
        public void Crb65_InvalidScore_UnexpectedValue_Throw(int score)
        {
            Assert.Throws<InvalidOperationException>(() => _service.Crb65(score, []));
        }

        #endregion

        #region Test Psi

        [Theory]
        [InlineData(0, Severity.Mild, TreatmentSite.Outpatient)] // Boundary value
        [InlineData(1, Severity.Mild, TreatmentSite.Outpatient)]
        [InlineData(65, Severity.Mild, TreatmentSite.Outpatient)]
        [InlineData(30, Severity.Mild, TreatmentSite.Outpatient)]
        [InlineData(70, Severity.Mild, TreatmentSite.Outpatient)] // Boundary value
        [InlineData(71, Severity.Moderate, TreatmentSite.Inpatient)] // Boundary value
        [InlineData(80, Severity.Moderate, TreatmentSite.Inpatient)]
        [InlineData(89, Severity.Moderate, TreatmentSite.Inpatient)]
        [InlineData(90, Severity.Moderate, TreatmentSite.Inpatient)] // Boundary value
        [InlineData(91, Severity.Severe, TreatmentSite.Inpatient)] // Boundary value
        [InlineData(100, Severity.Severe, TreatmentSite.Inpatient)]
        [InlineData(129, Severity.Severe, TreatmentSite.Inpatient)]
        [InlineData(130, Severity.Severe, TreatmentSite.Inpatient)] // Boundary value
        [InlineData(131, Severity.Severe, TreatmentSite.IntensiveCareUnit)] // Boundary value
        [InlineData(140, Severity.Severe, TreatmentSite.IntensiveCareUnit)]
        [InlineData(150, Severity.Severe, TreatmentSite.IntensiveCareUnit)]
        [InlineData(100000, Severity.Severe, TreatmentSite.IntensiveCareUnit)]
        public void Psi_Success(int score, Severity severity, TreatmentSite treatmentSite)
        {
            var result = _service.Psi(score, []);
            Assert.Equal(severity, result.Severity);
            Assert.Equal(treatmentSite, result.TreatmentSite);
        }

        [Theory]
        [InlineData(-1)] // Boundary value
        [InlineData(-132)]
        [InlineData(-67)]
        public void Psi_InvalidScore_Negative_Throw(int score)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => _service.Psi(score, []));
        }

        #endregion

        #region IDSA/ATS test

        public static readonly TheoryData<List<ClinicalObservation>, int, int> IdsaAts_NeedIcu_Case = [
            // Boundary value: 1 major criterion
            new([Obs("INVASIVE-MECHANICAL-VENTILATION")], 1, 0),
            // Boundary value: 3 minor criterion
            new(
                [
                    Obs("RR", 32),
                    Obs("CONFUSION"),
                    Obs("HYPOTENSION")
                ], 0, 3
            ),
            // Boundary value: 1 major and 3 minor criterion
            new(
                [
                    // PaO2/FiO2 ~ 214
                    Obs("PAO2", 45),
                    Obs("FIO2", 21m),
                    Obs("WBC", 3999),
                    Obs("PLATELET", 99_999),
                    Obs("SEPTIC-SHOCK-REQUIRE-VASOPRESSOR")
                ], 1, 3
            ),
            // Boundary value: 2 major and 2 minor criterion
            new(
                [
                    // PaO2/FiO2 ~ 214
                    Obs("PAO2", 45),
                    Obs("FIO2", 21m),
                    Obs("WBC", 4999),
                    Obs("PLATELET", 99_999),
                    Obs("SEPTIC-SHOCK-REQUIRE-VASOPRESSOR"),
                    Obs("INVASIVE-MECHANICAL-VENTILATION")
                ], 2, 2
            ),
        ];

        [Theory]
        [MemberData(nameof(IdsaAts_NeedIcu_Case))]
        public void IdsaAts_NeedIcu_Success(List<ClinicalObservation> observations, int majorMatched, int minorMatched)
        {
            var metric = _context.Metrics.FirstOrDefault(x => x.Code.Equals("IDSA/ATS")) ?? throw new Exception("IDSA/ATS metric not found");
            var diagnosis = _service.IdsaAts(metric, observations);
            Assert.NotNull(diagnosis);
            Assert.Equal(majorMatched, diagnosis.MajorMatched);
            Assert.Equal(minorMatched, diagnosis.MinorMatched);
            Assert.Equal(Severity.Severe, diagnosis.Severity);
            Assert.Equal(TreatmentSite.IntensiveCareUnit, diagnosis.TreatmentSite);
            Assert.NotEmpty(diagnosis.Evidences);
            Assert.Equal("IDSA/ATS", diagnosis.Code);
        }

        [Fact]
        public void IdsaAts_NoCriterionMatch_DoNotNeedIcu_Success()
        {
            var metric = _context.Metrics.FirstOrDefault(x => x.Code.Equals("IDSA/ATS")) ?? throw new Exception("IDSA/ATS metric not found");
            var diagnosis = _service.IdsaAts(metric, []);
            Assert.Null(diagnosis);
        }

        [Fact]
        public void IdsaAts_NotMajorMinorMetric_Throw()
        {
            var metric = _context.Metrics.FirstOrDefault(x => x.Code.Equals("PSI")) ?? throw new Exception("PSI metric not found");
            Assert.Throws<ArgumentException>(() => _service.IdsaAts(metric, []));
        }

        #endregion

        #region Test AssessInfection

        [Theory]
        [InlineData(Severity.Mild, TreatmentSite.Outpatient)]
        [InlineData(Severity.Moderate, TreatmentSite.Inpatient)]
        [InlineData(Severity.Severe, TreatmentSite.Inpatient)]
        [InlineData(Severity.Severe, TreatmentSite.IntensiveCareUnit)]
        public void AssessInfection_NoRiskFactor_Success(Severity severity, TreatmentSite treatmentSite)
        {
            var assessment = _service.AssessInfection(_context, [], severity, treatmentSite);
            Assert.Empty(assessment.HeavySuspected);
            Assert.NotEmpty(assessment.WorthSuspected);

            // Get the list of suspected pathogens based on the assessment
            var suspected = _context.SuspectedCauses
                .Where(sc => sc.Severity == severity && sc.TreatmentSite == treatmentSite)
                .Select(sc => sc.PathogenId)
                .ToHashSet();

            Assert.Equal(suspected.Count, assessment.WorthSuspected.Count());
            Assert.True(assessment.WorthSuspected.All(ws => suspected.Contains(ws.Id)));
        }

        public static readonly TheoryData<List<ClinicalObservation>, Severity, TreatmentSite, List<Pathogen>> AssessInfectionCase_RiskFactor_NotInSuspected = [
            new(
                [
                    Obs("PRIOR-PSEUDOMONAS"),
                ],
                Severity.Mild,
                TreatmentSite.Outpatient,
                [
                    Pathogen("Pseudomonas aeruginosa")
                ]),
            new(
                [
                    Obs("PRIOR-SKIN-INFECTION"),
                    Obs("PRIOR-PSEUDOMONAS"),
                    Obs("CENTRAL-VENOUS-CATHETER-PLACEMENT"),
                    Obs("PRIOR-MRSA-INFECTION")
                ],
                Severity.Moderate,
                TreatmentSite.Inpatient,
                [
                    Pathogen("Methicillin-Resistant Staphylococcus aureus (MRSA)"),
                    Pathogen("Pseudomonas aeruginosa")
                ]),
            new(
                [
                    Obs("PRIOR-MRSA-INFECTION"),
                    Obs("PRIOR-PSEUDOMONAS"),
                ],
                Severity.Severe,
                TreatmentSite.Inpatient,
                [
                    Pathogen("Methicillin-Resistant Staphylococcus aureus (MRSA)"),
                    Pathogen("Pseudomonas aeruginosa")
                ]),
            new(
                [
                    Obs("AGE", 61),
                    Obs("SMOKING"),
                    Obs("ALCOHOL-ABUSE"),
                    Obs("COPD"),
                    Obs("DIABETES")
                ],
                Severity.Severe,
                TreatmentSite.IntensiveCareUnit,
                [
                    Pathogen("Acinetobacter baumannii"),
                    Pathogen("Haemophilus influenzae")
                ]),
        ];

        [Theory]
        [MemberData(nameof(AssessInfectionCase_RiskFactor_NotInSuspected))]
        public void AssessInfection_WithRiskFactor_NotInSuspected_Success(
            List<ClinicalObservation> observations,
            Severity severity,
            TreatmentSite treatmentSite,
            List<Pathogen> expected)
        {
            var assessment = _service.AssessInfection(_context, observations, severity, treatmentSite);
            Assert.NotEmpty(assessment.HeavySuspected);
            Assert.NotEmpty(assessment.WorthSuspected);

            // Get the list of suspected pathogens based on the assessment
            var suspected = _context.SuspectedCauses
                .Where(sc => sc.Severity == severity && sc.TreatmentSite == treatmentSite)
                .Select(sc => sc.PathogenId)
                .ToHashSet();

            Assert.Equal(suspected.Count, assessment.WorthSuspected.Count());
            Assert.True(assessment.WorthSuspected.All(ws => suspected.Contains(ws.Id)));

            // Check for heavy suspected
            Assert.Equal(expected.Count, assessment.HeavySuspected.Count());
            Assert.True(expected.All(x => assessment.HeavySuspected.Select(hs => hs.Id).Contains(x.Id)));
        }

        [Fact]
        public void AssessInfection_WithRiskFactor_ScoreBelow50_NotReturn()
        {
            List<ClinicalObservation> observations = [Obs("AGE", 61)];
            var assessment = _service.AssessInfection(_context, observations, Severity.Mild, TreatmentSite.Outpatient);
            Assert.Empty(assessment.HeavySuspected);
        }

        #endregion

        #region Test IsAntibioticGroupHasCommonAncestor

        public static readonly TheoryData<AntibioticGroup, AntibioticGroup, bool> IsAncestorCase = [
            new(AntibioticGroup("Penicilin"), AntibioticGroup("Beta-lactam"), true),
            new(AntibioticGroup("Các beta-lactam khác"), AntibioticGroup("Carbapenem"), true),
            new(AntibioticGroup("Monobactam"), AntibioticGroup("Cephalosporin"), true),
            new(AntibioticGroup("Aminoglycosid"), AntibioticGroup("Peptid"), false),
        ];

        [Theory]
        [MemberData(nameof(IsAncestorCase))]
        public void IsAntibioticGroupHasCommonAncestor_Success(AntibioticGroup group1, AntibioticGroup group2, bool expected)
        {
            var result = _service.AreAntibioticGroupsHaveCommonAncestor(_context, group1, group2);
            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Success, result.StatusCode);
            Assert.Equal(expected, result.Data);
        }

        public static readonly TheoryData<AntibioticGroup, AntibioticGroup> IsAncestorCase_NotInContext = [
            new(
                new AntibioticGroup
                {
                    Name = "Group 1",
                    Description = "Group 1 description",
                    ParentId = null
                },
                AntibioticGroup("Beta-lactam")),
            new(
                AntibioticGroup("Các beta-lactam khác"),
                new AntibioticGroup
                {
                    Name = "Group 2",
                    Description = "Group 2 description",
                    ParentId = null
                }),
            new(
                new AntibioticGroup
                {
                    Name = "Group 3",
                    Description = "Group 3 description",
                    ParentId = null
                }, new AntibioticGroup
                {
                    Name = "Group 4",
                    Description = "Group 4 description",
                    ParentId = null
                }),
        ];

        [Theory]
        [MemberData(nameof(IsAncestorCase_NotInContext))]
        public void IsAntibioticGroupHasCommonAncestor_GroupNotInContext_Failure(AntibioticGroup group1, AntibioticGroup group2)
        {
            var result = _service.AreAntibioticGroupsHaveCommonAncestor(_context, group1, group2);
            Assert.True(result.IsFailure());
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.BadRequest, result.Error.StatusCode);
        }

        #endregion

        #region IsMedicineCombinationValid

        public static readonly TheoryData<string[], bool> Combination_Valid = [
            new(["Cefotaxim", "Moxifloxacin", "Erythromycin"], true),
            new(["Cefotaxim", "Moxifloxacin", "Erythromycin", "Linezolid"], true),
            new(["Cefotaxim", "Moxifloxacin"], true),
            new(["Cefotaxim", "Ceftazidim", "Penicilin G (Benzylpenicillin)"], false),
            new(["Cefotaxim", "Amoxicillin", "Penicilin G (Benzylpenicillin)"], false),
            new(["Cefotaxim"], true),
        ];

        [Theory]
        [MemberData(nameof(Combination_Valid))]
        public void IsMedicineCombinationValid_Success(string[] antibioticNames, bool expected)
        {
            var antibiotics = new List<Antibiotic>();
            foreach (var name in antibioticNames)
            {
                antibiotics.Add(Antibiotic(name));
            }
            var treatment = new Treatment
            {
                TreatmentProtocolId = Guid.CreateVersion7(),
                Combination = antibiotics
            };

            var result = _service.IsMedicineCombinationValid(_context, treatment);
            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Success, result.StatusCode);
            Assert.Equal(expected, result.Data);
        }

        #endregion

        #region Test ValidateObservations

        #region ValidateObservations - Happy path

        [Fact]
        public void ValidateObservations_AllRequiredVariablesValid_Success()
        {
            var result = _service.ValidateObservations(_context, ValidRequiredObservations());

            Assert.True(result.IsSuccess());
            Assert.Equal(ApplicationStatus.Success, result.StatusCode);
            Assert.Null(result.Error);
        }

        [Fact]
        public void ValidateObservations_WithOptionalVariablesValid_Success()
        {
            var observations = ValidRequiredObservations();
            observations.AddRange([
                Obs("CREATINE", 88),          // umol/L, normal renal function
                Obs("UREA", 6.5m),            // mmol/L
                Obs("PAO2", 78),              // mmHg, mild hypoxemia
                Obs("WBC", 11_500),           // cells/mm3, mild leukocytosis
                Obs("PLATELET", 245_000),     // cells/mm3
                Obs("PH", 7.38m),             // arterial blood gas
                Obs("HEMATOCRIT", 41.5m),     // %
                Obs("NA+", 138),              // mmol/L
                Obs("BLOOD-GLUCOSE", 6.4m),   // mmol/L
                Obs("CHRONIC-LIVER", false),
                Obs("PLEURAL-EFFUSION", false),
                Obs("MULTILOBAR-PULMONARY-LESION", true),
                Obs("ABSCESS-ON-CHEST-X-RAY", false),
            ]);

            var result = _service.ValidateObservations(_context, observations);

            Assert.True(result.IsSuccess());
            Assert.Equal(ApplicationStatus.Success, result.StatusCode);
            Assert.Null(result.Error);
        }

        [Theory]
        [InlineData("female")]       // accepted after normalization by IsValidValue
        [InlineData("Female")]
        [InlineData("  FEMALE  ")]
        public void ValidateObservations_CategoricalValueIsNormalized_Success(string gender)
        {
            var observations = WithObservation(ValidRequiredObservations(), Obs("GENDER", gender));

            var result = _service.ValidateObservations(_context, observations);

            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
        }

        [Theory]
        [InlineData("AGE", 16)]           // inclusive minimum
        [InlineData("AGE", 120)]          // realistic extreme old age (no upper bound)
        [InlineData("WEIGHT", 0.1)]       // just above exclusive minimum
        [InlineData("TEMPERATURE", 30.1)] // just above exclusive minimum (severe hypothermia)
        [InlineData("FIO2", 21)]          // inclusive minimum (room air)
        [InlineData("FIO2", 100)]         // inclusive maximum
        [InlineData("SPO2", 0)]           // inclusive minimum
        [InlineData("SPO2", 100)]         // inclusive maximum
        [InlineData("PH", 0)]             // inclusive minimum
        [InlineData("PH", 14)]            // inclusive maximum
        [InlineData("HEMATOCRIT", 0)]     // inclusive minimum
        [InlineData("HEMATOCRIT", 100)]   // inclusive maximum
        public void ValidateObservations_NumericValueAtBoundary_Success(string code, double value)
        {
            var observations = WithObservation(ValidRequiredObservations(), Obs(code, (decimal)value));

            var result = _service.ValidateObservations(_context, observations);

            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
        }

        #endregion

        #region ValidateObservations - Missing required variables

        [Theory]
        [InlineData("AGE")]
        [InlineData("GENDER")]
        [InlineData("SBP")]
        [InlineData("PREGNANT-OR-LACTATING")]
        public void ValidateObservations_MissingRequiredVariable_Failure(string code)
        {
            var observations = WithoutVariable(ValidRequiredObservations(), code);

            var result = _service.ValidateObservations(_context, observations);

            Assert.True(result.IsFailure());
            Assert.Equal(ApplicationStatus.BadRequest, result.StatusCode);
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.BadRequest, result.Error.StatusCode);

            // Missing required variables is a client integration problem, so the service
            // intentionally does not return per-variable details for this case
            Assert.Null(result.Error.Detail);
        }

        [Fact]
        public void ValidateObservations_NoObservations_Failure()
        {
            var result = _service.ValidateObservations(_context, []);

            Assert.True(result.IsFailure());
            Assert.Equal(ApplicationStatus.BadRequest, result.StatusCode);
            Assert.NotNull(result.Error);
            Assert.Null(result.Error.Detail);
        }

        [Fact]
        public void ValidateObservations_OnlyOptionalVariables_Failure()
        {
            // Supplying optional variables must not satisfy the required variables rule
            var result = _service.ValidateObservations(_context, [Obs("CREATINE", 88), Obs("UREA", 6.5m)]);

            Assert.True(result.IsFailure());
            Assert.Equal(ApplicationStatus.BadRequest, result.StatusCode);
            Assert.NotNull(result.Error);
            Assert.Null(result.Error.Detail);
        }

        #endregion

        #region ValidateObservations - Invalid observation value

        [Theory]
        [InlineData("AGE", 15)]           // below inclusive minimum 16 (adolescent, outside adult protocol)
        [InlineData("WEIGHT", 0)]         // exclusive minimum (weight measurement not recorded)
        [InlineData("TEMPERATURE", 30)]   // exclusive minimum (profound hypothermia)
        [InlineData("FIO2", 20)]          // below minimum 21 (lower than room air)
        [InlineData("FIO2", 100.1)]       // just above inclusive maximum
        [InlineData("SPO2", 101)]         // just above inclusive maximum (invalid oximetry reading)
        [InlineData("SPO2", -1)]          // below inclusive minimum (impossible reading)
        [InlineData("PH", 14.1)]          // just above inclusive maximum
        [InlineData("HEMATOCRIT", 101)]   // just above inclusive maximum
        [InlineData("DBP", 0)]            // exclusive minimum
        public void ValidateObservations_NumericValueOutOfRange_Failure(string code, double value)
        {
            var variable = Variable(code);
            var observations = WithObservation(ValidRequiredObservations(), Obs(code, (decimal)value));

            var result = _service.ValidateObservations(_context, observations);

            var details = AssertInvalidObservationsFailure(result);
            Assert.Single(details);
            Assert.True(details.ContainsKey(variable.Id));
        }

        [Fact]
        public void ValidateObservations_NumericValueMissing_Failure()
        {
            // A required variable is present but the client did not send its value
            var age = Variable("AGE");
            var observation = new ClinicalObservation(age, 72) { NumericValue = null };
            var observations = WithObservation(ValidRequiredObservations(), observation);

            var result = _service.ValidateObservations(_context, observations);

            var details = AssertInvalidObservationsFailure(result);
            Assert.Single(details);
            Assert.True(details.ContainsKey(age.Id));
        }

        [Theory]
        [InlineData("UNKNOWN")]
        [InlineData("NON-BINARY")]
        [InlineData("MALE FEMALE")]
        [InlineData("")]
        public void ValidateObservations_InvalidCategoricalValue_Failure(string value)
        {
            var gender = Variable("GENDER");
            var observations = WithObservation(ValidRequiredObservations(), Obs("GENDER", value));

            var result = _service.ValidateObservations(_context, observations);

            var details = AssertInvalidObservationsFailure(result);
            Assert.Single(details);
            Assert.True(details.ContainsKey(gender.Id));
        }

        [Fact]
        public void ValidateObservations_InvalidBooleanValue_Failure()
        {
            // A boolean observation can only be invalid when its value is missing. The constructor
            // prevents this, so simulate a client payload that omits the boolean value.
            var confusion = Variable("CONFUSION");
            var observation = new ClinicalObservation(confusion, true) { BooleanValue = null };
            var observations = WithObservation(ValidRequiredObservations(), observation);

            var result = _service.ValidateObservations(_context, observations);

            var details = AssertInvalidObservationsFailure(result);
            Assert.Single(details);
            Assert.True(details.ContainsKey(confusion.Id));
        }

        [Fact]
        public void ValidateObservations_MultipleInvalidValues_Failure()
        {
            var age = Variable("AGE");
            var spo2 = Variable("SPO2");
            var gender = Variable("GENDER");

            var observations = ValidRequiredObservations();
            observations = WithObservation(observations, Obs("AGE", 15));
            observations = WithObservation(observations, Obs("SPO2", 101));
            observations = WithObservation(observations, Obs("GENDER", "UNKNOWN"));

            var result = _service.ValidateObservations(_context, observations);

            // Every invalid observation must be reported, not just the first one
            var details = AssertInvalidObservationsFailure(result);
            Assert.Equal(3, details.Count);
            Assert.Contains(age.Id, details.Keys);
            Assert.Contains(spo2.Id, details.Keys);
            Assert.Contains(gender.Id, details.Keys);
        }

        #endregion

        #region ValidateObservations - Prerequisite formula

        [Theory]
        [InlineData(false, "MALE")]   // not pregnant, male
        [InlineData(false, "FEMALE")] // not pregnant, female
        [InlineData(true, "FEMALE")]  // pregnant female
        public void ValidateObservations_PrerequisiteSatisfied_Success(bool pregnant, string gender)
        {
            var observations = WithObservation(ValidRequiredObservations(), Obs("PREGNANT-OR-LACTATING", pregnant));
            observations = WithObservation(observations, Obs("GENDER", gender));

            var result = _service.ValidateObservations(_context, observations);

            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
        }

        [Fact]
        public void ValidateObservations_PrerequisiteNotSatisfied_Failure()
        {
            // PREGNANT-OR-LACTATING implies the patient must be female
            var pregnant = Variable("PREGNANT-OR-LACTATING");
            var observations = WithObservation(ValidRequiredObservations(), Obs("PREGNANT-OR-LACTATING", true));
            observations = WithObservation(observations, Obs("GENDER", "MALE"));

            var result = _service.ValidateObservations(_context, observations);

            var details = AssertInvalidObservationsFailure(result);
            Assert.Single(details);
            Assert.True(details.ContainsKey(pregnant.Id));
        }

        [Fact]
        public void ValidateObservations_PrerequisiteVariableNotProvided_Failure()
        {
            // The seed context only has a prerequisite whose variables are all required, so build
            // a minimal context where a prerequisite depends on a non-required variable
            var prerequisiteVariable = new BooleanClinicalVariable
            {
                Id = Guid.CreateVersion7(),
                Code = "GENDER-IS-FEMALE",
                Name = "Patient is female",
                Description = "Whether the patient is female",
                IsRequired = false,
                Category = ClinicalVariableCategory.PersonalInformation,
            };

            var requiredVariable = new BooleanClinicalVariable
            {
                Id = Guid.CreateVersion7(),
                Code = "PREGNANT-OR-LACTATING",
                Name = "Pregnant or lactating",
                Description = "Whether the patient is pregnant or lactating",
                IsRequired = true,
                Category = ClinicalVariableCategory.MedicalHistory,
                Prerequisite = new VariableFormula(prerequisiteVariable),
            };

            var context = new ClinicalContext
            {
                Variables = [requiredVariable, prerequisiteVariable],
                Metrics = [],
                Pathogens = [],
                SuspectedCauses = [],
                AntibioticGroups = [],
                Antibiotics = [],
                Antibiograms = [],
                TreatmentProtocols = [],
            };

            var result = _service.ValidateObservations(context, [new ClinicalObservation(requiredVariable, false)]);

            // The prerequisite cannot be evaluated because one of its variables was not supplied
            var details = AssertInvalidObservationsFailure(result);
            Assert.Single(details);
            Assert.True(details.ContainsKey(requiredVariable.Id));
        }

        [Fact]
        public void ValidateObservations_PrerequisiteWithLowercaseCategoricalValue_Success()
        {
            // "female" is an accepted value (categorical values are normalized by IsValidValue),
            // so it must also satisfy the prerequisite GENDER = FEMALE
            var observations = WithObservation(ValidRequiredObservations(), Obs("PREGNANT-OR-LACTATING", true));
            observations = WithObservation(observations, Obs("GENDER", "female"));

            var result = _service.ValidateObservations(_context, observations);

            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
        }

        #endregion

        #endregion

        #region Test Diagnose

        #region Diagnose - Severity and treatment site

        [Fact]
        public void Diagnose_AllMetricsAgreeMild_Success()
        {
            // 64 years old is just below the CURB-65/CRB-65 age threshold
            var observations = WithObservation(ValidRequiredObservations(), Obs("AGE", 64));

            var result = _service.Diagnose(_context, observations);

            Assert.True(result.IsSuccess());
            Assert.Equal(ApplicationStatus.Success, result.StatusCode);
            Assert.Null(result.Error);

            var diagnosis = result.Data!;
            Assert.Equal(Severity.Mild, diagnosis.Severity);
            Assert.Equal(TreatmentSite.Outpatient, diagnosis.TreatmentSite);

            // CURB-65, CRB-65 and PSI agree on Mild/Outpatient; IDSA/ATS is not satisfied
            // so it must not be included in the per-metric diagnosis list
            var metricDiagnoses = diagnosis.MetricsSeverityDiagnosis.ToList();
            Assert.Equal(3, metricDiagnoses.Count);
            Assert.DoesNotContain(metricDiagnoses, d => d.Code.Equals("IDSA/ATS"));

            var curb = Assert.IsType<ScoreMetricsSeverityDiagnosis>(metricDiagnoses.Single(d => d.Code.Equals("CURB-65")));
            Assert.Equal(0, curb.Score);
            Assert.Equal(Severity.Mild, curb.Severity);
            Assert.Equal(TreatmentSite.Outpatient, curb.TreatmentSite);

            var crb = Assert.IsType<ScoreMetricsSeverityDiagnosis>(metricDiagnoses.Single(d => d.Code.Equals("CRB-65")));
            Assert.Equal(0, crb.Score);
            Assert.Equal(Severity.Mild, crb.Severity);
            Assert.Equal(TreatmentSite.Outpatient, crb.TreatmentSite);

            // PSI for a 64 years old male with no other risk factor is exactly his age
            var psi = Assert.IsType<ScoreMetricsSeverityDiagnosis>(metricDiagnoses.Single(d => d.Code.Equals("PSI")));
            Assert.Equal(64, psi.Score);
            Assert.Equal(Severity.Mild, psi.Severity);
            Assert.Equal(TreatmentSite.Outpatient, psi.TreatmentSite);
        }

        [Fact]
        public void Diagnose_MetricsDisagree_SelectsHighestSeverity_Success()
        {
            // Baseline patient (72 years old): CURB-65 = 1 (Mild/Outpatient) while
            // CRB-65 = 1 (Moderate/Inpatient) and PSI = 72 (Moderate/Inpatient).
            // The final result must be the most severe one, not the first/last metric.
            var result = _service.Diagnose(_context, ValidRequiredObservations());

            Assert.True(result.IsSuccess());
            var diagnosis = result.Data!;
            Assert.Equal(Severity.Moderate, diagnosis.Severity);
            Assert.Equal(TreatmentSite.Inpatient, diagnosis.TreatmentSite);

            var curb = Assert.IsType<ScoreMetricsSeverityDiagnosis>(diagnosis.MetricsSeverityDiagnosis.Single(d => d.Code.Equals("CURB-65")));
            Assert.Equal(1, curb.Score);
            Assert.Equal(Severity.Mild, curb.Severity);
            Assert.Equal(TreatmentSite.Outpatient, curb.TreatmentSite);

            var crb = Assert.IsType<ScoreMetricsSeverityDiagnosis>(diagnosis.MetricsSeverityDiagnosis.Single(d => d.Code.Equals("CRB-65")));
            Assert.Equal(1, crb.Score);
            Assert.Equal(Severity.Moderate, crb.Severity);
            Assert.Equal(TreatmentSite.Inpatient, crb.TreatmentSite);

            var psi = Assert.IsType<ScoreMetricsSeverityDiagnosis>(diagnosis.MetricsSeverityDiagnosis.Single(d => d.Code.Equals("PSI")));
            Assert.Equal(72, psi.Score);
            Assert.Equal(Severity.Moderate, psi.Severity);
            Assert.Equal(TreatmentSite.Inpatient, psi.TreatmentSite);
        }

        [Fact]
        public void Diagnose_SevereInpatient_Success()
        {
            var observations = WithObservation(ValidRequiredObservations(), Obs("AGE", 64));
            observations = WithObservation(observations, Obs("CONFUSION"));
            observations = WithObservation(observations, Obs("RR", 30));  // boundary: >= 30
            observations = WithObservation(observations, Obs("SBP", 89)); // boundary: < 90

            var result = _service.Diagnose(_context, observations);

            Assert.True(result.IsSuccess());
            var diagnosis = result.Data!;
            Assert.Equal(Severity.Severe, diagnosis.Severity);
            Assert.Equal(TreatmentSite.Inpatient, diagnosis.TreatmentSite);

            // CURB-65 = confusion + RR + low SBP = 3 (UREA is not supplied);
            // CRB-65 = confusion + RR + low SBP = 3
            Assert.Equal(3, Assert.IsType<ScoreMetricsSeverityDiagnosis>(diagnosis.MetricsSeverityDiagnosis.Single(d => d.Code.Equals("CURB-65"))).Score);
            Assert.Equal(3, Assert.IsType<ScoreMetricsSeverityDiagnosis>(diagnosis.MetricsSeverityDiagnosis.Single(d => d.Code.Equals("CRB-65"))).Score);

            // PSI = age 64 + confusion 20 + RR 20 + low SBP 20 = 124
            var psi = Assert.IsType<ScoreMetricsSeverityDiagnosis>(diagnosis.MetricsSeverityDiagnosis.Single(d => d.Code.Equals("PSI")));
            Assert.Equal(124, psi.Score);
            Assert.Equal(Severity.Severe, psi.Severity);
            Assert.Equal(TreatmentSite.Inpatient, psi.TreatmentSite);

            // Only 2 IDSA/ATS minor criteria are matched, so ICU is not required
            Assert.DoesNotContain(diagnosis.MetricsSeverityDiagnosis, d => d.Code.Equals("IDSA/ATS"));
        }

        [Fact]
        public void Diagnose_PsiSevere_SelectsIcu_Success()
        {
            var observations = WithObservation(ValidRequiredObservations(), Obs("AGE", 64));
            observations = WithObservation(observations, Obs("RR", 30));                  // boundary: >= 30
            observations = WithObservation(observations, Obs("SBP", 89));                 // boundary: < 90
            observations = WithObservation(observations, Obs("TEMPERATURE", 40));         // boundary: >= 40
            observations = WithObservation(observations, Obs("PULSE", 125));              // boundary: >= 125
            observations = WithObservation(observations, Obs("PH", 7.30m));
            observations = WithObservation(observations, Obs("UREA", 11));                // BUN = 30.8 mg/dL
            observations = WithObservation(observations, Obs("NA+", 129));                // boundary: < 130
            observations = WithObservation(observations, Obs("BLOOD-GLUCOSE", 250));      // boundary: >= 250
            observations = WithObservation(observations, Obs("HEMATOCRIT", 29));          // boundary: < 30
            observations = WithObservation(observations, Obs("PAO2", 55));                // boundary: < 60
            observations = WithObservation(observations, Obs("PLEURAL-EFFUSION"));

            var result = _service.Diagnose(_context, observations);

            Assert.True(result.IsSuccess());
            var diagnosis = result.Data!;

            // CURB-65 and CRB-65 only ask for Inpatient, but PSI > 130 requires ICU.
            // Same severity with a higher treatment site must win the tie-break.
            Assert.Equal(Severity.Severe, diagnosis.Severity);
            Assert.Equal(TreatmentSite.IntensiveCareUnit, diagnosis.TreatmentSite);

            Assert.Equal(3, Assert.IsType<ScoreMetricsSeverityDiagnosis>(diagnosis.MetricsSeverityDiagnosis.Single(d => d.Code.Equals("CURB-65"))).Score);
            Assert.Equal(2, Assert.IsType<ScoreMetricsSeverityDiagnosis>(diagnosis.MetricsSeverityDiagnosis.Single(d => d.Code.Equals("CRB-65"))).Score);

            // PSI = age 64 + RR 20 + low SBP 20 + fever 15 + tachycardia 10 + acidosis 30
            //        + BUN 20 + hyponatremia 20 + hyperglycemia 10 + anemia 10 + hypoxemia 10
            //        + pleural effusion 10 = 239
            var psi = Assert.IsType<ScoreMetricsSeverityDiagnosis>(diagnosis.MetricsSeverityDiagnosis.Single(d => d.Code.Equals("PSI")));
            Assert.Equal(239, psi.Score);
            Assert.Equal(Severity.Severe, psi.Severity);
            Assert.Equal(TreatmentSite.IntensiveCareUnit, psi.TreatmentSite);
        }

        [Fact]
        public void Diagnose_IdsaAtsMajorCriterion_SelectsIcu_Success()
        {
            var observations = WithObservation(ValidRequiredObservations(), Obs("AGE", 64));
            observations = WithObservation(observations, Obs("INVASIVE-MECHANICAL-VENTILATION"));

            var result = _service.Diagnose(_context, observations);

            Assert.True(result.IsSuccess());
            var diagnosis = result.Data!;
            Assert.Equal(Severity.Severe, diagnosis.Severity);
            Assert.Equal(TreatmentSite.IntensiveCareUnit, diagnosis.TreatmentSite);

            // CURB-65, CRB-65, PSI and IDSA/ATS all produce a diagnosis
            var metricDiagnoses = diagnosis.MetricsSeverityDiagnosis.ToList();
            Assert.Equal(4, metricDiagnoses.Count);

            var idsa = Assert.IsType<MajorMinorMetricsSeverityDiagnosis>(metricDiagnoses.Single(d => d.Code.Equals("IDSA/ATS")));
            Assert.Equal(1, idsa.MajorMatched);
            Assert.Equal(0, idsa.MinorMatched);
            Assert.Equal(Severity.Severe, idsa.Severity);
            Assert.Equal(TreatmentSite.IntensiveCareUnit, idsa.TreatmentSite);

            // Every IDSA/ATS rule must produce an evidence flagged as major or minor
            Assert.Equal(Metric("IDSA/ATS").Rules.Count, idsa.Evidences.Count());
            Assert.Contains(idsa.Evidences, e => e.IsMajor == true);
            Assert.Contains(idsa.Evidences, e => e.IsMajor == false);
        }

        [Fact]
        public void Diagnose_IdsaAtsThreeMinorCriteria_SelectsIcu_Success()
        {
            var observations = WithObservation(ValidRequiredObservations(), Obs("AGE", 64));
            observations = WithObservation(observations, Obs("WBC", 3_999));           // boundary: < 4000
            observations = WithObservation(observations, Obs("PLATELET", 99_999));     // boundary: < 100000
            observations = WithObservation(observations, Obs("TEMPERATURE", 35.9m));   // boundary: < 36

            var result = _service.Diagnose(_context, observations);

            Assert.True(result.IsSuccess());
            var diagnosis = result.Data!;
            Assert.Equal(Severity.Severe, diagnosis.Severity);
            Assert.Equal(TreatmentSite.IntensiveCareUnit, diagnosis.TreatmentSite);

            var idsa = Assert.IsType<MajorMinorMetricsSeverityDiagnosis>(diagnosis.MetricsSeverityDiagnosis.Single(d => d.Code.Equals("IDSA/ATS")));
            Assert.Equal(0, idsa.MajorMatched);
            Assert.Equal(3, idsa.MinorMatched);

            // The three score metrics stay mild, proving IDSA/ATS alone can raise the final result
            Assert.All(
                diagnosis.MetricsSeverityDiagnosis.Where(d => !d.Code.Equals("IDSA/ATS")),
                d => Assert.Equal(Severity.Mild, d.Severity));
        }

        #endregion

        #region Diagnose - Infection assessment

        [Fact]
        public void Diagnose_NoRiskFactor_WorthSuspectedNotEmpty_Success()
        {
            var observations = WithObservation(ValidRequiredObservations(), Obs("AGE", 64));

            var result = _service.Diagnose(_context, observations);

            var diagnosis = result.Data!;
            Assert.Equal(Severity.Mild, diagnosis.Severity);
            Assert.Equal(TreatmentSite.Outpatient, diagnosis.TreatmentSite);

            // No risk factor reaches 50%, so nothing is heavily suspected
            Assert.Empty(diagnosis.HeavySuspected);

            // Worth suspected must contain every pathogen mapped to the final severity/site
            var expected = SuspectedPathogenIds(Severity.Mild, TreatmentSite.Outpatient);
            Assert.NotEmpty(expected);
            Assert.Equal(expected.Count, diagnosis.WorthSuspected.Count());
            Assert.All(diagnosis.WorthSuspected, p => Assert.Contains(p.Id, expected));
        }

        [Fact]
        public void Diagnose_CriticalRiskFactor_HeavySuspectedAndRemovedFromWorth_Success()
        {
            var observations = WithObservation(ValidRequiredObservations(), Obs("AGE", 64));
            observations = WithObservation(observations, Obs("INVASIVE-MECHANICAL-VENTILATION"));
            observations = WithObservation(observations, Obs("PRIOR-PSEUDOMONAS"));

            var result = _service.Diagnose(_context, observations);

            var diagnosis = result.Data!;
            Assert.Equal(Severity.Severe, diagnosis.Severity);
            Assert.Equal(TreatmentSite.IntensiveCareUnit, diagnosis.TreatmentSite);

            var pseudomonas = Pathogen("Pseudomonas aeruginosa");
            _ = Assert.Single(diagnosis.HeavySuspected);
            Assert.Equal(pseudomonas.Id, diagnosis.HeavySuspected.Single().Id);

            // A pathogen that is both heavily and worth suspected must only stay in the heavy list
            Assert.DoesNotContain(diagnosis.WorthSuspected, p => p.Id == pseudomonas.Id);
            var expected = SuspectedPathogenIds(Severity.Severe, TreatmentSite.IntensiveCareUnit);
            expected.Remove(pseudomonas.Id);
            Assert.Equal(expected.Count, diagnosis.WorthSuspected.Count());
            Assert.All(diagnosis.WorthSuspected, p => Assert.Contains(p.Id, expected));

            // The risk factor evaluation must be exposed as evidence
            Assert.Contains(diagnosis.Evidences, e => e.Source.StartsWith("Pseudomonas aeruginosa - "));
        }

        [Fact]
        public void Diagnose_NonCriticalRiskFactorsBelowThreshold_NotHeavySuspected_Success()
        {
            // AGE 64 + smoking matches 2 of 5 Acinetobacter baumannii risk factors (40% < 50%)
            var observations = WithObservation(ValidRequiredObservations(), Obs("AGE", 64));
            observations = WithObservation(observations, Obs("SMOKING"));

            var result = _service.Diagnose(_context, observations);

            Assert.Empty(result.Data!.HeavySuspected);
        }

        [Fact]
        public void Diagnose_NonCriticalRiskFactorsAtThreshold_HeavySuspected_Success()
        {
            // AGE 64 + smoking + alcohol abuse matches 3 of 5 Acinetobacter baumannii
            // risk factors (60% >= 50%), while Haemophilus influenzae only matches 3 of 7 (42.8%)
            var observations = WithObservation(ValidRequiredObservations(), Obs("AGE", 64));
            observations = WithObservation(observations, Obs("SMOKING"));
            observations = WithObservation(observations, Obs("ALCOHOL-ABUSE"));

            var result = _service.Diagnose(_context, observations);

            var diagnosis = result.Data!;
            var acinetobacter = Pathogen("Acinetobacter baumannii");
            var haemophilus = Pathogen("Haemophilus influenzae");

            _ = Assert.Single(diagnosis.HeavySuspected);
            Assert.Equal(acinetobacter.Id, diagnosis.HeavySuspected.Single().Id);
            Assert.DoesNotContain(diagnosis.HeavySuspected, p => p.Id == haemophilus.Id);
        }

        [Fact]
        public void Diagnose_HeavySuspectedPathogenNotInWorthList_Success()
        {
            // MRSA is not mapped to any severity/treatment site in the suspected causes,
            // so a matched critical risk factor must add it to the heavy list only
            var observations = WithObservation(ValidRequiredObservations(), Obs("AGE", 64));
            observations = WithObservation(observations, Obs("PRIOR-MRSA-INFECTION"));

            var result = _service.Diagnose(_context, observations);

            var diagnosis = result.Data!;
            Assert.Equal(Severity.Mild, diagnosis.Severity);
            Assert.Equal(TreatmentSite.Outpatient, diagnosis.TreatmentSite);

            var mrsa = Pathogen("Methicillin-Resistant Staphylococcus aureus (MRSA)");
            _ = Assert.Single(diagnosis.HeavySuspected);
            Assert.Equal(mrsa.Id, diagnosis.HeavySuspected.Single().Id);

            // Nothing can be removed from the worth suspected list, it stays untouched
            var expected = SuspectedPathogenIds(Severity.Mild, TreatmentSite.Outpatient);
            Assert.DoesNotContain(mrsa.Id, expected);
            Assert.Equal(expected.Count, diagnosis.WorthSuspected.Count());
            Assert.All(diagnosis.WorthSuspected, p => Assert.Contains(p.Id, expected));
        }

        #endregion

        #region Diagnose - Missing variables

        [Fact]
        public void Diagnose_OptionalVariablesMissing_ReportsMissingVariables_Success()
        {
            var result = _service.Diagnose(_context, ValidRequiredObservations());

            var diagnosis = result.Data!;
            var optional = _context.Variables.Where(v => !v.IsRequired).ToList();

            Assert.NotEmpty(optional);
            Assert.Equal(optional.Count, diagnosis.MissingVariables.Count());
            Assert.All(optional, v => Assert.Contains(diagnosis.MissingVariables, m => m.Id == v.Id));
            Assert.DoesNotContain(diagnosis.MissingVariables, m => m.Code.Equals("AGE"));
        }

        [Fact]
        public void Diagnose_AllVariablesProvided_NoMissingVariables_Success()
        {
            var observations = ValidRequiredObservations();
            observations.AddRange([
                Obs("PAO2", 78),
                Obs("PLEURAL-EFFUSION", false),
                Obs("UREA", 6.5m),
                Obs("CREATINE", 88),
                Obs("WBC", 11_500),
                Obs("PLATELET", 245_000),
                Obs("PH", 7.38m),
                Obs("HEMATOCRIT", 41.5m),
                Obs("NA+", 138),
                Obs("BLOOD-GLUCOSE", 6.4m),
                Obs("MULTILOBAR-PULMONARY-LESION", false),
                Obs("ABSCESS-ON-CHEST-X-RAY", false),
            ]);

            var result = _service.Diagnose(_context, observations);

            Assert.True(result.IsSuccess());
            Assert.Empty(result.Data!.MissingVariables);
        }

        #endregion

        #region Diagnose - eGFR

        [Fact]
        public void Diagnose_WithoutCreatinine_EgfrNull_Success()
        {
            var result = _service.Diagnose(_context, ValidRequiredObservations());

            Assert.True(result.IsSuccess());
            Assert.Null(result.Data!.Egfr);
        }

        [Theory]
        [InlineData(72, "MALE", false, 88d, 73.8d)]
        [InlineData(72, "FEMALE", true, 88d, 66.4d)]   // female and black american corrections
        [InlineData(16, "MALE", false, 44d, 223.0d)]   // AGE at the accepted minimum
        [InlineData(16, "MALE", false, 700d, 9.2d)]    // dialysis-level creatinine in a young patient
        public void Diagnose_WithCreatinine_CalculatesEgfr_Success(
            int age,
            string gender,
            bool isBlackAmerican,
            double creatine,
            double expected)
        {
            var observations = WithObservation(ValidRequiredObservations(), Obs("AGE", age));
            observations = WithObservation(observations, Obs("GENDER", gender));
            observations = WithObservation(observations, Obs("BLACK-AMERICAN", isBlackAmerican));
            observations = WithObservation(observations, Obs("CREATINE", (decimal)creatine));

            var result = _service.Diagnose(_context, observations);

            Assert.True(result.IsSuccess());
            Assert.NotNull(result.Data!.Egfr);
            Assert.Equal(expected, result.Data.Egfr!.Value, 1);
        }

        [Fact]
        public void Diagnose_WithCreatinineMissingGender_Throws()
        {
            var observations = WithoutVariable(ValidRequiredObservations(), "GENDER");
            observations.Add(Obs("CREATINE", 88));

            Assert.Throws<InvalidOperationException>(() => _service.Diagnose(_context, observations));
        }

        [Fact]
        public void Diagnose_WithCreatinineMissingBlackAmerican_Throws()
        {
            var observations = WithoutVariable(ValidRequiredObservations(), "BLACK-AMERICAN");
            observations.Add(Obs("CREATINE", 88));

            Assert.Throws<InvalidOperationException>(() => _service.Diagnose(_context, observations));
        }

        [Fact]
        public void Diagnose_WithCreatinineMissingAge_Throws()
        {
            var observations = WithoutVariable(ValidRequiredObservations(), "AGE");
            observations.Add(Obs("CREATINE", 88));

            // Use CURB-65 only: PSI's score function itself needs AGE and would throw
            // before the eGFR block is reached
            var context = ContextWithMetrics(Metric("CURB-65"));

            Assert.Throws<InvalidOperationException>(() => _service.Diagnose(context, observations));
        }

        [Fact]
        public void Diagnose_WithLowercaseFemaleGender_CalculatesFemaleEgfr_Success()
        {
            // "female" is a valid categorical value, so the female correction must be applied
            var observations = WithObservation(ValidRequiredObservations(), Obs("GENDER", "female"));
            observations = WithObservation(observations, Obs("CREATINE", 88));

            var result = _service.Diagnose(_context, observations);

            Assert.True(result.IsSuccess());
            Assert.NotNull(result.Data!.Egfr);
            // 175 * (88 / 88.4)^-1.154 * 72^-0.203 * 0.742
            Assert.Equal(54.8, result.Data.Egfr!.Value, 1);
        }

        #endregion

        #region Diagnose - Pregnancy status

        [Theory]
        [InlineData("MALE", false, false)]
        [InlineData("FEMALE", false, false)]
        [InlineData("FEMALE", true, true)]
        public void Diagnose_PregnancyStatus_Success(string gender, bool pregnant, bool expected)
        {
            var observations = WithObservation(ValidRequiredObservations(), Obs("GENDER", gender));
            observations = WithObservation(observations, Obs("PREGNANT-OR-LACTATING", pregnant));

            var result = _service.Diagnose(_context, observations);

            Assert.True(result.IsSuccess());
            Assert.Equal(expected, result.Data!.IsPatientPregnantOrInLactationPhase);
        }

        [Fact]
        public void Diagnose_PregnancyVariableMissing_NotPregnant_Success()
        {
            // The variable is optional for the diagnosis, so a missing value must be
            // treated as "not pregnant or lactating" instead of failing
            var observations = WithoutVariable(ValidRequiredObservations(), "PREGNANT-OR-LACTATING");

            var result = _service.Diagnose(_context, observations);

            Assert.True(result.IsSuccess());
            Assert.False(result.Data!.IsPatientPregnantOrInLactationPhase);
        }

        #endregion

        #region Diagnose - Metric handling and failure

        [Fact]
        public void Diagnose_UnsupportedMetric_Ignored_Success()
        {
            var context = ContextWithMetrics(Metric("CURB-65"), UnsupportedMetric());
            var observations = WithObservation(ValidRequiredObservations(), Obs("AGE", 64));

            var result = _service.Diagnose(context, observations);

            Assert.True(result.IsSuccess());
            var diagnosis = result.Data!;
            Assert.Equal(Severity.Mild, diagnosis.Severity);
            Assert.Equal(TreatmentSite.Outpatient, diagnosis.TreatmentSite);

            // The unsupported metric must be skipped without affecting the supported ones
            _ = Assert.Single(diagnosis.MetricsSeverityDiagnosis);
            Assert.Equal("CURB-65", diagnosis.MetricsSeverityDiagnosis.Single().Code);
        }

        [Fact]
        public void Diagnose_NoSupportedMetric_Failure()
        {
            var context = ContextWithMetrics(UnsupportedMetric());

            var result = _service.Diagnose(context, ValidRequiredObservations());

            Assert.True(result.IsFailure());
            Assert.Equal(ApplicationStatus.ServerError, result.StatusCode);
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.ServerError, result.Error.StatusCode);
            Assert.Null(result.Data);
        }

        [Fact]
        public void Diagnose_NoMetrics_Failure()
        {
            var context = ContextWithMetrics();

            var result = _service.Diagnose(context, ValidRequiredObservations());

            Assert.True(result.IsFailure());
            Assert.Equal(ApplicationStatus.ServerError, result.StatusCode);
            Assert.NotNull(result.Error);
        }

        #endregion

        #region Diagnose - Evidence

        [Fact]
        public void Diagnose_EvidencesContainEveryMetricRuleAndRiskFactor_Success()
        {
            var observations = WithObservation(ValidRequiredObservations(), Obs("AGE", 64));

            var result = _service.Diagnose(_context, observations);

            var diagnosis = result.Data!;
            var metricRuleCount =
                Metric("CURB-65").Rules.Count +
                Metric("CRB-65").Rules.Count +
                Metric("PSI").Rules.Count;
            var riskFactorCount = _context.Pathogens
                .Where(p => p.RiskFactors.Count > 0)
                .Sum(p => p.RiskFactors.Count);

            // Every rule of every applied metric and every risk factor must produce an evidence
            Assert.Equal(metricRuleCount + riskFactorCount, diagnosis.Evidences.Count());
            Assert.All(diagnosis.Evidences, e =>
            {
                Assert.False(string.IsNullOrWhiteSpace(e.Source));
                Assert.False(string.IsNullOrWhiteSpace(e.Formula));
            });

            foreach (var metricCode in new[] { "CURB-65", "CRB-65", "PSI" })
            {
                var metricDiagnosis = Assert.IsType<ScoreMetricsSeverityDiagnosis>(
                    diagnosis.MetricsSeverityDiagnosis.Single(d => d.Code.Equals(metricCode)));

                Assert.Equal(Metric(metricCode).Rules.Count, metricDiagnosis.Evidences.Count());
                Assert.All(metricDiagnosis.Evidences, e => Assert.NotNull(e.Score));
            }
        }

        #endregion

        #endregion

        #region Test GetMicrobiologicalTestResult

        #region GetMicrobiologicalTestResult - Matching

        [Fact]
        public void GetMicrobiologicalTestResult_ExactMatch_ReturnsAntibiogram_Success()
        {
            var pathogen = Pathogen("Streptococcus pneumoniae");
            var antibiogram = Antibiogram(
                "Streptococcus pneumoniae",
                MicCategory.Susceptible,
                "Penicilin G (Benzylpenicillin)",
                "Amoxicillin");
            var context = ContextWithAntibiograms(antibiogram);

            var result = _service.GetMicrobiologicalTestResult(
                context,
                [Sample("Streptococcus pneumoniae", MicCategory.Susceptible, MicrobialSampleType.Sputum)]);

            Assert.True(result.IsSuccess());
            Assert.Equal(ApplicationStatus.Success, result.StatusCode);
            Assert.Null(result.Error);

            // The exact antibiogram instance must be returned, with its susceptibility profile
            var returned = Assert.Single(result.Data!);
            Assert.Same(antibiogram, returned);
            Assert.Equal(pathogen.Id, returned.PathogenId);
            Assert.Equal(MicCategory.Susceptible, returned.MicCategory);
            Assert.Equal(2, returned.Antibiotics.Count);
            Assert.Contains(returned.Antibiotics, a => a.Name.Equals("Amoxicillin"));
        }

        [Theory]
        [InlineData(MicCategory.Susceptible)]
        [InlineData(MicCategory.Intermediate)]
        [InlineData(MicCategory.Resistant)]
        public void GetMicrobiologicalTestResult_MatchesMicCategory_Success(MicCategory micCategory)
        {
            // The same pathogen can have a different antibiogram per MIC category,
            // so the sample's category must select the right one
            var antibiograms = new[]
            {
                Antibiogram("Klebsiella pneumoniae", MicCategory.Susceptible, "Meropenem"),
                Antibiogram("Klebsiella pneumoniae", MicCategory.Intermediate, "Ceftriaxon"),
                Antibiogram("Klebsiella pneumoniae", MicCategory.Resistant, "Amikacin"),
            };
            var context = ContextWithAntibiograms(antibiograms);

            var result = _service.GetMicrobiologicalTestResult(
                context,
                [Sample("Klebsiella pneumoniae", micCategory)]);

            Assert.True(result.IsSuccess());
            var returned = Assert.Single(result.Data!);
            Assert.Same(antibiograms.Single(a => a.MicCategory == micCategory), returned);
            Assert.Equal(micCategory, returned.MicCategory);
        }

        [Fact]
        public void GetMicrobiologicalTestResult_MultipleSamples_ReturnsInSampleOrder_Success()
        {
            var pneumococcus = Antibiogram("Streptococcus pneumoniae", MicCategory.Susceptible, "Amoxicillin");
            var klebsiella = Antibiogram("Klebsiella pneumoniae", MicCategory.Resistant, "Meropenem");
            var context = ContextWithAntibiograms(pneumococcus, klebsiella);

            // The sample type is not part of the match: only pathogen and MIC category are
            var result = _service.GetMicrobiologicalTestResult(
                context,
                [
                    Sample("Klebsiella pneumoniae", MicCategory.Resistant, MicrobialSampleType.BronchoalveolarLavageFluid),
                    Sample("Streptococcus pneumoniae", MicCategory.Susceptible, MicrobialSampleType.Sputum),
                ]);

            Assert.True(result.IsSuccess());
            var returned = result.Data!.ToList();
            Assert.Equal(2, returned.Count);
            Assert.Same(klebsiella, returned[0]);
            Assert.Same(pneumococcus, returned[1]);
        }

        #endregion

        #region GetMicrobiologicalTestResult - Unmatched samples

        [Fact]
        public void GetMicrobiologicalTestResult_NoAntibiogramForMicCategory_SkipsSample_Success()
        {
            var susceptible = Antibiogram("Pseudomonas aeruginosa", MicCategory.Susceptible, "Ceftazidim");
            var context = ContextWithAntibiograms(susceptible);

            var result = _service.GetMicrobiologicalTestResult(
                context,
                [Sample("Pseudomonas aeruginosa", MicCategory.Resistant)]);

            // A sample without a matching antibiogram is skipped, not an error
            Assert.True(result.IsSuccess());
            Assert.Equal(ApplicationStatus.Success, result.StatusCode);
            Assert.Empty(result.Data!);
        }

        [Fact]
        public void GetMicrobiologicalTestResult_NoAntibiogramForPathogen_SkipsSample_Success()
        {
            var context = ContextWithAntibiograms(
                Antibiogram("Klebsiella pneumoniae", MicCategory.Susceptible, "Meropenem"));

            var result = _service.GetMicrobiologicalTestResult(
                context,
                [Sample("Escherichia coli (E. coli)", MicCategory.Susceptible)]);

            Assert.True(result.IsSuccess());
            Assert.Empty(result.Data!);
        }

        [Fact]
        public void GetMicrobiologicalTestResult_MixedSamples_ReturnsOnlyMatched_Success()
        {
            var matched = Antibiogram("Klebsiella pneumoniae", MicCategory.Intermediate, "Cefotaxim");
            var context = ContextWithAntibiograms(matched);

            var result = _service.GetMicrobiologicalTestResult(
                context,
                [
                    Sample("Staphylococcus aureus", MicCategory.Susceptible),
                    Sample("Klebsiella pneumoniae", MicCategory.Intermediate),
                    Sample("Klebsiella pneumoniae", MicCategory.Resistant),
                ]);

            Assert.True(result.IsSuccess());
            var returned = Assert.Single(result.Data!);
            Assert.Same(matched, returned);
        }

        [Fact]
        public void GetMicrobiologicalTestResult_InvalidMicCategory_SkipsSample_Success()
        {
            var context = ContextWithAntibiograms(
                Antibiogram("Klebsiella pneumoniae", MicCategory.Susceptible, "Meropenem"));

            // 99 is not a defined MicCategory, so it can never match a persisted antibiogram
            var result = _service.GetMicrobiologicalTestResult(
                context,
                [Sample("Klebsiella pneumoniae", (MicCategory)99)]);

            Assert.True(result.IsSuccess());
            Assert.Empty(result.Data!);
        }

        #endregion

        #region GetMicrobiologicalTestResult - Empty input

        [Fact]
        public void GetMicrobiologicalTestResult_NoSamples_ReturnsEmpty_Success()
        {
            var context = ContextWithAntibiograms(
                Antibiogram("Klebsiella pneumoniae", MicCategory.Susceptible, "Meropenem"));

            var result = _service.GetMicrobiologicalTestResult(context, []);

            Assert.True(result.IsSuccess());
            Assert.Equal(ApplicationStatus.Success, result.StatusCode);
            Assert.Empty(result.Data!);
        }

        [Fact]
        public void GetMicrobiologicalTestResult_NoAntibiograms_ReturnsEmpty_Success()
        {
            var context = ContextWithAntibiograms();

            var result = _service.GetMicrobiologicalTestResult(
                context,
                [Sample("Klebsiella pneumoniae", MicCategory.Susceptible)]);

            Assert.True(result.IsSuccess());
            Assert.Empty(result.Data!);
        }

        #endregion

        #endregion

        #region Test RecommendTreatment

        #region RecommendTreatment - Candidate selection

        [Fact]
        public void RecommendTreatment_NoProtocolForDiagnosis_Failure()
        {
            var context = ContextWithProtocols(
                Protocol(Severity.Mild, TreatmentSite.Outpatient, [], [], ["Amoxicillin"]));

            // The only protocol is for a mild outpatient, so a severe ICU diagnosis has no candidate
            var result = _service.RecommendTreatment(
                context,
                Picture(ValidRequiredObservations()),
                DiagnosisFor(Severity.Severe, TreatmentSite.IntensiveCareUnit));

            Assert.True(result.IsFailure());
            Assert.Equal(ApplicationStatus.Failure, result.StatusCode);
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.Failure, result.Error.StatusCode);
            Assert.Null(result.Data);
        }

        [Fact]
        public void RecommendTreatment_InvalidDiagnosisSeverity_Failure()
        {
            var context = ContextWithProtocols(
                Protocol(Severity.Mild, TreatmentSite.Outpatient, [], [], ["Amoxicillin"]));

            // 99 is not a defined Severity, so it cannot match any persisted protocol
            var result = _service.RecommendTreatment(
                context,
                Picture(ValidRequiredObservations()),
                DiagnosisFor((Severity)99, TreatmentSite.Outpatient));

            Assert.True(result.IsFailure());
            Assert.Equal(ApplicationStatus.Failure, result.StatusCode);
            Assert.Null(result.Data);
        }

        [Fact]
        public void RecommendTreatment_MatchingSeverityAndSite_ReturnsOnlyMatching_Success()
        {
            var mildA = Protocol(Severity.Mild, TreatmentSite.Outpatient, [], [], ["Amoxicillin"]);
            var mildB = Protocol(Severity.Mild, TreatmentSite.Outpatient, [], [], ["Doxycyclin"]);
            var severe = Protocol(Severity.Severe, TreatmentSite.IntensiveCareUnit, [], [], ["Meropenem"]);
            var context = ContextWithProtocols(mildA, mildB, severe);

            var result = _service.RecommendTreatment(
                context,
                Picture(ValidRequiredObservations()),
                DiagnosisFor(Severity.Mild, TreatmentSite.Outpatient));

            Assert.True(result.IsSuccess());
            Assert.Equal(ApplicationStatus.Success, result.StatusCode);
            Assert.Null(result.Error);

            var protocols = result.Data!.ToList();
            Assert.Equal(2, protocols.Count);
            Assert.Same(mildA, protocols[0]);
            Assert.Same(mildB, protocols[1]);
        }

        [Fact]
        public void RecommendTreatment_SeedProtocols_ReturnsOnlyApplicable_Success()
        {
            // Real diagnosis for a 64 years old patient without risk factors:
            // Mild/Outpatient and no heavily suspected pathogen
            var observations = WithObservation(ValidRequiredObservations(), Obs("AGE", 64));
            var diagnosis = _service.Diagnose(_context, observations).Data!;
            Assert.Empty(diagnosis.HeavySuspected);

            var result = _service.RecommendTreatment(_context, Picture(observations), diagnosis);

            Assert.True(result.IsSuccess());
            var protocols = result.Data!.ToList();

            // Only the generic Mild/Outpatient protocol applies: the pathogen-specific
            // protocols require a heavily suspected pathogen, and the criteria-based
            // protocol requires an immunocompromised patient
            var protocol = Assert.Single(protocols);
            Assert.Equal(Severity.Mild, protocol.Severity);
            Assert.Equal(TreatmentSite.Outpatient, protocol.TreatmentSite);

            // The generic protocol offers 3 single-antibiotic alternatives
            Assert.Equal(3, protocol.Treatments.Count);
            var names = protocol.Treatments.SelectMany(t => t.Combination).Select(a => a.Name).ToList();
            Assert.Contains("Amoxicillin", names);
            Assert.Contains("Levofloxacin", names);
            Assert.Contains("Moxifloxacin", names);
        }

        #endregion

        #region RecommendTreatment - Criteria filtering

        [Theory]
        [InlineData(59, 0)] // boundary: just below AGE >= 60
        [InlineData(60, 1)] // boundary: exactly AGE >= 60
        [InlineData(72, 1)]
        public void RecommendTreatment_AgeCriterionBoundary_Success(int age, int expectedCount)
        {
            var protocol = Protocol(
                Severity.Mild,
                TreatmentSite.Outpatient,
                [],
                [AgeAtLeastCriterion(60)],
                ["Amoxicillin"]);
            var observations = WithObservation(ValidRequiredObservations(), Obs("AGE", age));

            var result = _service.RecommendTreatment(
                ContextWithProtocols(protocol),
                Picture(observations),
                DiagnosisFor(Severity.Mild, TreatmentSite.Outpatient));

            Assert.True(result.IsSuccess());
            Assert.Equal(expectedCount, result.Data!.Count());
        }

        [Fact]
        public void RecommendTreatment_AllCriteriaMustMatch_RemovesProtocol_Success()
        {
            // AGE 72 satisfies the first criterion, but IMMUNOCOMPROMISED is false in the
            // baseline, so the protocol's AND condition is not satisfied
            var protocol = Protocol(
                Severity.Mild,
                TreatmentSite.Outpatient,
                [],
                [AgeAtLeastCriterion(60), BooleanCriterion("IMMUNOCOMPROMISED", true)],
                ["Amoxicillin"]);

            var result = _service.RecommendTreatment(
                ContextWithProtocols(protocol),
                Picture(ValidRequiredObservations()),
                DiagnosisFor(Severity.Mild, TreatmentSite.Outpatient));

            // No candidate survives the criteria filter, but that is not an error
            Assert.True(result.IsSuccess());
            Assert.Empty(result.Data!);
        }

        [Fact]
        public void RecommendTreatment_CriterionVariableMissing_RemovesProtocol_Success()
        {
            // The criterion variable is not observed, so the criterion cannot be satisfied
            var observations = WithoutVariable(ValidRequiredObservations(), "IMMUNOCOMPROMISED");
            var protocol = Protocol(
                Severity.Mild,
                TreatmentSite.Outpatient,
                [],
                [BooleanCriterion("IMMUNOCOMPROMISED", true)],
                ["Amoxicillin"]);

            var result = _service.RecommendTreatment(
                ContextWithProtocols(protocol),
                Picture(observations),
                DiagnosisFor(Severity.Mild, TreatmentSite.Outpatient));

            Assert.True(result.IsSuccess());
            Assert.Empty(result.Data!);
        }

        [Theory]
        [InlineData(false, 1)]
        [InlineData(true, 2)]
        public void RecommendTreatment_SeedProtocolCriteria_IncludedOnlyWhenSatisfied_Success(
            bool immunocompromised,
            int expectedCount)
        {
            var observations = WithObservation(ValidRequiredObservations(), Obs("AGE", 64));
            observations = WithObservation(observations, Obs("IMMUNOCOMPROMISED", immunocompromised));
            var diagnosis = _service.Diagnose(_context, observations).Data!;
            Assert.Empty(diagnosis.HeavySuspected);

            var result = _service.RecommendTreatment(_context, Picture(observations), diagnosis);

            Assert.True(result.IsSuccess());
            var protocols = result.Data!.ToList();
            Assert.Equal(expectedCount, protocols.Count);

            // The seed data has a single protocol with criteria (elderly + immunocompromised)
            var criteriaProtocol = _context.TreatmentProtocols.Single(p => p.Criteria.Count > 0);
            Assert.Equal(
                immunocompromised,
                criteriaProtocol.Criteria.All(c => c.IsCriterionSatisfied(observations)));

            if (immunocompromised)
            {
                Assert.Contains(protocols, p => p.Id == criteriaProtocol.Id);
            }
            else
            {
                Assert.DoesNotContain(protocols, p => p.Id == criteriaProtocol.Id);
            }
        }

        #endregion

        #region RecommendTreatment - Pathogen filtering

        [Fact]
        public void RecommendTreatment_RequiredPathogenNotHeavySuspected_RemovesProtocol_Success()
        {
            var protocol = Protocol(
                Severity.Mild,
                TreatmentSite.Outpatient,
                [Pathogen("Pseudomonas aeruginosa")],
                [],
                ["Ceftazidim"]);

            var result = _service.RecommendTreatment(
                ContextWithProtocols(protocol),
                Picture(ValidRequiredObservations()),
                DiagnosisFor(Severity.Mild, TreatmentSite.Outpatient));

            Assert.True(result.IsSuccess());
            Assert.Empty(result.Data!);
        }

        [Fact]
        public void RecommendTreatment_RequiredPathogenHeavySuspected_KeepsProtocol_Success()
        {
            var protocol = Protocol(
                Severity.Mild,
                TreatmentSite.Outpatient,
                [Pathogen("Pseudomonas aeruginosa")],
                [],
                ["Ceftazidim"]);

            var result = _service.RecommendTreatment(
                ContextWithProtocols(protocol),
                Picture(ValidRequiredObservations()),
                DiagnosisFor(Severity.Mild, TreatmentSite.Outpatient, Pathogen("Pseudomonas aeruginosa")));

            Assert.True(result.IsSuccess());
            Assert.Same(protocol, Assert.Single(result.Data!));
        }

        [Fact]
        public void RecommendTreatment_AllRequiredPathogensMustBeHeavySuspected_RemovesProtocol_Success()
        {
            // Pathogens is an AND list: Pseudomonas is suspected but MRSA is not
            var protocol = Protocol(
                Severity.Severe,
                TreatmentSite.IntensiveCareUnit,
                [Pathogen("Pseudomonas aeruginosa"), Pathogen("Methicillin-Resistant Staphylococcus aureus (MRSA)")],
                [],
                ["Piperacillin/tazobactam", "Vancomycin"]);

            var result = _service.RecommendTreatment(
                ContextWithProtocols(protocol),
                Picture(ValidRequiredObservations()),
                DiagnosisFor(Severity.Severe, TreatmentSite.IntensiveCareUnit, Pathogen("Pseudomonas aeruginosa")));

            Assert.True(result.IsSuccess());
            Assert.Empty(result.Data!);
        }

        [Fact]
        public void RecommendTreatment_ProtocolWithoutPathogens_Kept_Success()
        {
            var generic = Protocol(Severity.Mild, TreatmentSite.Outpatient, [], [], ["Amoxicillin"]);
            var targeted = Protocol(
                Severity.Mild,
                TreatmentSite.Outpatient,
                [Pathogen("Pseudomonas aeruginosa")],
                [],
                ["Ceftazidim"]);

            var result = _service.RecommendTreatment(
                ContextWithProtocols(generic, targeted),
                Picture(ValidRequiredObservations()),
                DiagnosisFor(Severity.Mild, TreatmentSite.Outpatient, Pathogen("Pseudomonas aeruginosa")));

            // An empty pathogen list is satisfied vacuously, and dominance is not
            // applied because only one pathogen is heavily suspected
            Assert.True(result.IsSuccess());
            var protocols = result.Data!.ToList();
            Assert.Equal(2, protocols.Count);
            Assert.Contains(generic, protocols);
            Assert.Contains(targeted, protocols);
        }

        [Fact]
        public void RecommendTreatment_HeavySuspectedAtLeastTwo_DropsSubsetProtocols_Success()
        {
            var pseudomonas = Pathogen("Pseudomonas aeruginosa");
            var mrsa = Pathogen("Methicillin-Resistant Staphylococcus aureus (MRSA)");

            var pseudomonasOnly = Protocol(Severity.Severe, TreatmentSite.IntensiveCareUnit, [pseudomonas], [], ["Ceftazidim"]);
            var mrsaOnly = Protocol(Severity.Severe, TreatmentSite.IntensiveCareUnit, [mrsa], [], ["Vancomycin"]);
            var combined = Protocol(Severity.Severe, TreatmentSite.IntensiveCareUnit, [pseudomonas, mrsa], [], ["Piperacillin/tazobactam", "Vancomycin"]);
            var generic = Protocol(Severity.Severe, TreatmentSite.IntensiveCareUnit, [], [], ["Meropenem"]);

            var result = _service.RecommendTreatment(
                ContextWithProtocols(pseudomonasOnly, mrsaOnly, combined, generic),
                Picture(ValidRequiredObservations()),
                DiagnosisFor(Severity.Severe, TreatmentSite.IntensiveCareUnit, pseudomonas, mrsa));

            // The combined protocol covers both suspected pathogens, so every protocol
            // with a smaller pathogen set (including the generic one) is dropped
            Assert.True(result.IsSuccess());
            Assert.Same(combined, Assert.Single(result.Data!));
        }

        [Fact]
        public void RecommendTreatment_HeavySuspectedBelowTwo_KeepsSubsetProtocols_Success()
        {
            var pseudomonas = Pathogen("Pseudomonas aeruginosa");

            var pseudomonasOnly = Protocol(Severity.Mild, TreatmentSite.Outpatient, [pseudomonas], [], ["Ceftazidim"]);
            var generic = Protocol(Severity.Mild, TreatmentSite.Outpatient, [], [], ["Amoxicillin"]);

            var result = _service.RecommendTreatment(
                ContextWithProtocols(generic, pseudomonasOnly),
                Picture(ValidRequiredObservations()),
                DiagnosisFor(Severity.Mild, TreatmentSite.Outpatient, pseudomonas));

            // Dominance only makes sense with more than one heavily suspected pathogen
            Assert.True(result.IsSuccess());
            var protocols = result.Data!.ToList();
            Assert.Equal(2, protocols.Count);
            Assert.Contains(generic, protocols);
            Assert.Contains(pseudomonasOnly, protocols);
        }

        [Fact]
        public void RecommendTreatment_HeavySuspectedDisjointProtocols_KeepsBoth_Success()
        {
            var pseudomonas = Pathogen("Pseudomonas aeruginosa");
            var mrsa = Pathogen("Methicillin-Resistant Staphylococcus aureus (MRSA)");

            var pseudomonasOnly = Protocol(Severity.Severe, TreatmentSite.IntensiveCareUnit, [pseudomonas], [], ["Ceftazidim"]);
            var mrsaOnly = Protocol(Severity.Severe, TreatmentSite.IntensiveCareUnit, [mrsa], [], ["Vancomycin"]);

            var result = _service.RecommendTreatment(
                ContextWithProtocols(pseudomonasOnly, mrsaOnly),
                Picture(ValidRequiredObservations()),
                DiagnosisFor(Severity.Severe, TreatmentSite.IntensiveCareUnit, pseudomonas, mrsa));

            // Neither protocol covers the other, so both are valid candidates
            Assert.True(result.IsSuccess());
            var protocols = result.Data!.ToList();
            Assert.Equal(2, protocols.Count);
            Assert.Contains(pseudomonasOnly, protocols);
            Assert.Contains(mrsaOnly, protocols);
        }

        #endregion

        #region RecommendTreatment - Allergy filtering

        [Fact]
        public void RecommendTreatment_AllergicAntibiotic_RemovesComposition_Success()
        {
            var protocol = Protocol(
                Severity.Mild,
                TreatmentSite.Outpatient,
                [],
                [],
                ["Amoxicillin"],
                ["Doxycyclin"]);

            var result = _service.RecommendTreatment(
                ContextWithProtocols(protocol),
                Picture(ValidRequiredObservations(), Antibiotic("Amoxicillin")),
                DiagnosisFor(Severity.Mild, TreatmentSite.Outpatient));

            Assert.True(result.IsSuccess());
            var returned = Assert.Single(result.Data!);
            var treatment = Assert.Single(returned.Treatments);
            Assert.Contains(treatment.Combination, a => a.Name.Equals("Doxycyclin"));
            Assert.DoesNotContain(returned.Treatments.SelectMany(t => t.Combination), a => a.Name.Equals("Amoxicillin"));
        }

        [Fact]
        public void RecommendTreatment_AllergicAntibioticInCombination_RemovesWholeComposition_Success()
        {
            // The whole combination must be dropped, not only the offending antibiotic
            var protocol = Protocol(
                Severity.Moderate,
                TreatmentSite.Inpatient,
                [],
                [],
                ["Ceftriaxon", "Azithromycin"],
                ["Levofloxacin"]);

            var result = _service.RecommendTreatment(
                ContextWithProtocols(protocol),
                Picture(ValidRequiredObservations(), Antibiotic("Ceftriaxon")),
                DiagnosisFor(Severity.Moderate, TreatmentSite.Inpatient));

            Assert.True(result.IsSuccess());
            var returned = Assert.Single(result.Data!);
            var treatment = Assert.Single(returned.Treatments);
            Assert.Contains(treatment.Combination, a => a.Name.Equals("Levofloxacin"));
            Assert.DoesNotContain(returned.Treatments.SelectMany(t => t.Combination), a => a.Name.Equals("Azithromycin"));
        }

        [Fact]
        public void RecommendTreatment_AllTreatmentsAllergic_RemovesProtocol_Success()
        {
            var protocol = Protocol(
                Severity.Mild,
                TreatmentSite.Outpatient,
                [],
                [],
                ["Amoxicillin"]);

            var result = _service.RecommendTreatment(
                ContextWithProtocols(protocol),
                Picture(ValidRequiredObservations(), Antibiotic("Amoxicillin")),
                DiagnosisFor(Severity.Mild, TreatmentSite.Outpatient));

            // A protocol without any usable treatment serves no purpose
            Assert.True(result.IsSuccess());
            Assert.Empty(result.Data!);
        }

        [Fact]
        public void RecommendTreatment_MultipleAllergies_RemovesAllMatchingCompositions_Success()
        {
            var protocol = Protocol(
                Severity.Mild,
                TreatmentSite.Outpatient,
                [],
                [],
                ["Amoxicillin"],
                ["Doxycyclin"],
                ["Azithromycin"]);

            var result = _service.RecommendTreatment(
                ContextWithProtocols(protocol),
                Picture(ValidRequiredObservations(), Antibiotic("Amoxicillin"), Antibiotic("Doxycyclin")),
                DiagnosisFor(Severity.Mild, TreatmentSite.Outpatient));

            Assert.True(result.IsSuccess());
            var returned = Assert.Single(result.Data!);
            var treatment = Assert.Single(returned.Treatments);
            Assert.Contains(treatment.Combination, a => a.Name.Equals("Azithromycin"));
        }

        [Fact]
        public void RecommendTreatment_NoAllergy_KeepsAllTreatments_Success()
        {
            var protocol = Protocol(
                Severity.Mild,
                TreatmentSite.Outpatient,
                [],
                [],
                ["Amoxicillin"],
                ["Doxycyclin"],
                ["Azithromycin"]);

            var result = _service.RecommendTreatment(
                ContextWithProtocols(protocol),
                Picture(ValidRequiredObservations()),
                DiagnosisFor(Severity.Mild, TreatmentSite.Outpatient));

            Assert.True(result.IsSuccess());
            var returned = Assert.Single(result.Data!);
            Assert.Equal(3, returned.Treatments.Count);
        }

        [Fact]
        public void RecommendTreatment_AllergyOnOneProtocol_DoesNotAffectOther_Success()
        {
            var allergic = Protocol(Severity.Mild, TreatmentSite.Outpatient, [], [], ["Amoxicillin"]);
            var safe = Protocol(Severity.Mild, TreatmentSite.Outpatient, [], [], ["Doxycyclin"]);

            var result = _service.RecommendTreatment(
                ContextWithProtocols(allergic, safe),
                Picture(ValidRequiredObservations(), Antibiotic("Amoxicillin")),
                DiagnosisFor(Severity.Mild, TreatmentSite.Outpatient));

            // The protocol with only an allergic treatment is removed, the other stays intact
            Assert.True(result.IsSuccess());
            Assert.Same(safe, Assert.Single(result.Data!));
            Assert.Single(safe.Treatments);
        }

        #endregion

        #region RecommendTreatment - IsApplied filter

        [Fact]
        public void RecommendTreatment_OnlyNonAppliedProtocolMatches_Failure()
        {
            // The Mild/Outpatient protocol is disabled, so a Mild/Outpatient diagnosis
            // must fail instead of recommending it
            var notApplied = NonAppliedProtocol(Severity.Mild, TreatmentSite.Outpatient, ["Amoxicillin"]);
            var appliedElsewhere = Protocol(Severity.Severe, TreatmentSite.IntensiveCareUnit, [], [], ["Meropenem"]);

            var result = _service.RecommendTreatment(
                ContextWithProtocols(appliedElsewhere, notApplied),
                Picture(ValidRequiredObservations()),
                DiagnosisFor(Severity.Mild, TreatmentSite.Outpatient));

            Assert.True(result.IsFailure());
            Assert.Equal(ApplicationStatus.Failure, result.StatusCode);
            Assert.Null(result.Data);
        }

        [Fact]
        public void RecommendTreatment_MixedAppliedProtocols_KeepsOnlyApplied_Success()
        {
            var applied = Protocol(Severity.Mild, TreatmentSite.Outpatient, [], [], ["Amoxicillin"]);
            var notApplied = NonAppliedProtocol(Severity.Mild, TreatmentSite.Outpatient, ["Doxycyclin"]);

            var context = ContextWithProtocols(applied, notApplied);

            // Like clinical metrics, the clinical context filters out non-applied protocols
            Assert.DoesNotContain(context.TreatmentProtocols, p => p.Id == notApplied.Id);
            Assert.Contains(context.TreatmentProtocols, p => p.Id == applied.Id);

            var result = _service.RecommendTreatment(
                context,
                Picture(ValidRequiredObservations()),
                DiagnosisFor(Severity.Mild, TreatmentSite.Outpatient));

            Assert.True(result.IsSuccess());
            Assert.Same(applied, Assert.Single(result.Data!));
            Assert.DoesNotContain(result.Data!, p => !p.IsApplied);
        }

        #endregion

        #endregion

    }
}
