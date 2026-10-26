using System.Text.Json;
using Respira.Clinical.Domain.Enums;
using Respira.Clinical.Domain.Models;
using Respira.Clinical.Domain.Services;
using Respira.ServiceDefaults.Contracts.Results;
using Xunit;

namespace Respira.Clinical.Domain.Test.Services
{
    public record ObservationDto(string Code, string Value);
    public record ClinicalPictureDto
    {
        public required List<ObservationDto> Observations { get; set; }
    }

    public record DiagnosisDto
    {
        public required Severity Severity { get; set; }
        public required TreatmentSite TreatmentSite { get; set; }
        public List<string> HeavySuspected { get; set; } = [];

        // In real cases, worth suspected wouldn't be concerned much, since
        // worth suspected list is very broad, so it wouldn't be used that much
        // anyway (and we already have a test cover this function)
    }

    public record TestData
    {
        public required ClinicalPictureDto ClinicalPicture { get; set; }
        public required DiagnosisDto ExpectedDiagnosis { get; set; }
    }

    /// <summary>
    /// This class will test the <see cref="DiagnoseService.Diagnose" /> method.
    /// Since this class would require an enormous amount of test case, we will import
    /// the test data via JSON file instead of hard code test case like other tests.
    /// Note that, some input and output does not affect the diagnosis result (they are
    /// for the medicine recommendation, which is not cover by this test) like allergies,
    /// is pregnant,... so we will also won't handle them here
    /// </summary>
    public partial class DiagnoseServiceTest
    {
        private static ClinicalPicture MapClinicalPicture(ClinicalPictureDto dto)
        {
            // Map data
            var observations = dto.Observations.ConvertAll(o =>
            {
                // Get variable
                var variable = _context.Variables.FirstOrDefault(x => x.Code.Equals(o.Code, StringComparison.OrdinalIgnoreCase))
                    ?? throw new Exception($"Observation with variable code {o.Code} not found in context");

                // Try parse value
                if (variable.ValueType == ClinicalValueType.Boolean && bool.TryParse(o.Value, out var boolVal))
                {
                    return Obs(o.Code, boolVal);
                }

                if (variable.ValueType == ClinicalValueType.Numeric && decimal.TryParse(o.Value, out var numericVal))
                {
                    return Obs(o.Code, numericVal);
                }

                if (variable.ValueType == ClinicalValueType.Categorical)
                {
                    return Obs(o.Code, o.Value);
                }

                throw new Exception($"Failed to construct observation: type = {variable.ValueType}, value = {o.Value}");
            });

            return new ClinicalPicture
            {
                Allergies = [],
                Observations = observations
            };
        }

        private static Diagnosis MapDiagnosis(DiagnosisDto dto)
        {
            // Get pathogen list
            var heavySuspected = dto.HeavySuspected.ConvertAll(hs => Pathogen(hs)).ToList();

            return new Diagnosis
            {
                Severity = dto.Severity,
                TreatmentSite = dto.TreatmentSite,
                MetricsSeverityDiagnosis = [],
                Allergies = [],
                Evidences = [],
                HeavySuspected = heavySuspected,
                MissingVariables = [],
                WorthSuspected = [],
                IsPatientPregnantOrInLactationPhase = true
            };
        }

        private static (ClinicalPicture, Diagnosis) MapTestData(string filename)
        {
            // Load data from JSON
            var path = Path.IsPathRooted(filename)
                ? filename
                : Path.Combine(AppContext.BaseDirectory, filename);

            var json = File.ReadAllText(path);

            var dto = JsonSerializer.Deserialize<TestData>(json, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                ReadCommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true,
            }) ?? throw new Exception("Failed to deserialize clinical picture for testing diagnose feature");

            return (MapClinicalPicture(dto.ClinicalPicture), MapDiagnosis(dto.ExpectedDiagnosis));
        }

        [Theory]
        [InlineData("Data/TestCases/test-case-1.jsonc")]
        [InlineData("Data/TestCases/test-case-2.jsonc")]
        [InlineData("Data/TestCases/test-case-3.jsonc")]
        public void Diagnosis_Success(string filename)
        {
            var (input, expected) = MapTestData(filename);
            var result = _service.Diagnose(_context, input);

            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.NotNull(result.Data);
            Assert.Equal(ApplicationStatus.Success, result.StatusCode);

            Assert.Equal(expected.Severity, result.Data.Severity);
            Assert.Equal(expected.TreatmentSite, result.Data.TreatmentSite);
            Assert.Equal(expected.HeavySuspected.Count(), result.Data.HeavySuspected.Count());

            var resultHeavySuspected = result.Data.HeavySuspected.ToList();
            var expectedHeavySuspected = expected.HeavySuspected.ToList();

            for (var i = 0; i < expected.HeavySuspected.Count(); i++)
            {
                Assert.Equal(expectedHeavySuspected[i].Id, resultHeavySuspected[i].Id);
            }
        }
    }
}
