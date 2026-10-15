using Microsoft.Extensions.Logging;
using Moq;
using Respira.Clinical.Domain.Enums;
using Respira.Clinical.Domain.Models;
using Respira.Clinical.Domain.Services;
using Respira.Clinical.Infrastructure.Data;
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
    }
}
