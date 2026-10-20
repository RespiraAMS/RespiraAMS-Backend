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
            var seedData = DataSeeder.Load("seed-data-test.jsonc");
            return new ClinicalContext
            {
                Antibiotics = seedData.Antibiotics,
                Metrics = seedData.ClinicalMetrics,
                Pathogens = seedData.Pathogens,
                SuspectedCauses = seedData.SuspectedCauses,
                AntibioticGroups = seedData.AntibioticGroups,
                Variables = seedData.ClinicalVariables,
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
                Obs("RENAL-DISEASE")
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
        public void Curb65_WithBunPresent_Success(int score, Severity severity, TreatmentSite treatmentSite)
        {
            var result = _service.Curb65(score, [], false);
            Assert.Equal(severity, result.Severity);
            Assert.Equal(treatmentSite, result.TreatmentSite);
        }

        [Theory]
        [InlineData(0, Severity.Mild, TreatmentSite.Outpatient)]
        [InlineData(1, Severity.Moderate, TreatmentSite.Inpatient)]
        [InlineData(2, Severity.Moderate, TreatmentSite.Inpatient)]
        [InlineData(3, Severity.Severe, TreatmentSite.Inpatient)]
        [InlineData(4, Severity.Severe, TreatmentSite.IntensiveCareUnit)]
        public void Curb65_WithBunMissing_Success(int score, Severity severity, TreatmentSite treatmentSite)
        {
            var result = _service.Curb65(score, [], true);
            Assert.Equal(severity, result.Severity);
            Assert.Equal(treatmentSite, result.TreatmentSite);
        }

        [Theory]
        [InlineData(-1, true)] // Boundary value
        [InlineData(-10, true)]
        [InlineData(-1, false)] // Boundary value
        [InlineData(-10, false)]
        public void Curb65_InvalidScore_Negative_Throw(int score, bool isBunMissing)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => _service.Curb65(score, [], isBunMissing));
        }

        [Theory]
        [InlineData(5, true)] // Boundary value
        [InlineData(100, true)]
        [InlineData(6, false)] // Boundary value
        [InlineData(200, false)]
        public void Curb65_InvalidScore_UnexpectedValue_Throw(int score, bool isBunMissing)
        {
            Assert.Throws<InvalidOperationException>(() => _service.Curb65(score, [], isBunMissing));
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
            new([Obs("REQUIRES-MECHANICAL-VENTILATION")], 1, 0),
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
                    Obs("FIO2", 0.21m),
                    Obs("WBC", 3999),
                    Obs("PLATELET", 99_999),
                    Obs("SEPTIC-SHOCK")
                ], 1, 3
            ),
            // Boundary value: 2 major and 2 minor criterion
            new(
                [
                    // PaO2/FiO2 ~ 214
                    Obs("PAO2", 45),
                    Obs("FIO2", 0.21m),
                    Obs("WBC", 4999),
                    Obs("PLATELET", 99_999),
                    Obs("SEPTIC-SHOCK"),
                    Obs("REQUIRES-MECHANICAL-VENTILATION")
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

        public static readonly TheoryData<List<ClinicalObservation>, Severity, TreatmentSite, List<ScoredPathogen>> AssessInfectionCase_RiskFactor_NotInSuspected = [
            new(
                [
                    Obs("PRIOR-PSEUDOMONAS"),
                ],
                Severity.Mild,
                TreatmentSite.Outpatient,
                [
                    new(0.2m, Pathogen("Pseudomonas aeruginosa"))
                ]),
            new(
                [
                    Obs("PRIOR-SKIN-INFECTION"),
                    Obs("PRIOR-PSEUDOMONAS"),
                    Obs("CENTRAL-VENOUS-CATHETER-PLACEMENT")
                ],
                Severity.Moderate,
                TreatmentSite.Inpatient,
                [
                    new(0.2m, Pathogen("Staphylococcus aureus")),
                    new(0.2m, Pathogen("Pseudomonas aeruginosa"))
                ]),
            new(
                [
                    Obs("PRIOR-SKIN-INFECTION"),
                    Obs("PRIOR-PSEUDOMONAS"),
                ],
                Severity.Severe,
                TreatmentSite.Inpatient,
                [
                    new(0.1m, Pathogen("Staphylococcus aureus")),
                    new(0.2m, Pathogen("Pseudomonas aeruginosa"))
                ]),
            new(
                [
                    Obs("AGE", 61)
                ],
                Severity.Severe,
                TreatmentSite.IntensiveCareUnit,
                [
                    new(1m/3, Pathogen("Acinetobacter baumannii"))
                ]),
        ];

        [Theory]
        [MemberData(nameof(AssessInfectionCase_RiskFactor_NotInSuspected))]
        public void AssessInfection_WithRiskFactor_NotInSuspected_Success(
            List<ClinicalObservation> observations,
            Severity severity,
            TreatmentSite treatmentSite,
            List<ScoredPathogen> expected)
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
            expected = [.. expected.OrderByDescending(x => x.Score)];

            var heavySuspected = assessment.HeavySuspected.ToList();
            for (var i = 0; i < expected.Count; i++)
            {
                // In case of fraction like 1/3, we will allow for some precision,
                // though the test should use the fraction expression directly
                Assert.Equal(expected[i].Score, heavySuspected[i].Score, precision: 5);
            }
        }

        [Fact]
        public void AssessInfection_WithRiskFactor_InSuspected_ReceiveBoost_Success()
        {
            // In case of moderate/inpatient, Anaerobic bacteria is in the suspected list,
            // it also has a risk factor, so we choose it for test data of this case
            var assessment = _service.AssessInfection(_context, [Obs("OROPHARYNGEAL-ANAEROBIC")], Severity.Moderate, TreatmentSite.Inpatient);
            Assert.NotEmpty(assessment.WorthSuspected);
            Assert.NotEmpty(assessment.HeavySuspected);

            // Anaerobic bacteria should be in the heavy suspected list, and NOT in worth suspected list
            Assert.DoesNotContain(assessment.WorthSuspected, x => x.Id.Equals("Vi khuẩn kỵ khí"));
            var pathogen = Assert.Single(assessment.HeavySuspected, x => x.Pathogen.Name.Equals("Vi khuẩn kỵ khí"));

            // Score must receive a boost: 1 factor matched against total 5 factors,
            // plus a boost of 0.5, so the final score must be 0.7
            Assert.Equal((1m / 5) + DiagnoseService.InfectionBoostScore, pathogen.Score);
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
            var result = _service.IsAntibioticGroupHasCommonAncestor(_context, group1, group2);
            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Success, result.StatusCode);
            Assert.Equal(expected, result.Data);
        }

        #endregion
    }
}
