using System.Text.Json;
using Respira.Analysis.Domain.Entities;

namespace Respira.Analysis.Domain.Test.Entities
{
    /// <summary>
    /// The Analysis.Domain project does not reference Analysis.Application, so the
    /// <c>AnalysisReportResult</c> contract is not available here. The report
    /// payload is therefore verified through <see cref="JsonDocument"/> using the
    /// very same camelCase shape the application produces.
    /// </summary>
    public class ClinicalAnalysisJsonTest
    {
        private static readonly Guid AnalysisId = Guid.Parse(
            "40000000-0000-0000-0000-000000000001"
        );
        private static readonly Guid DoctorId = Guid.Parse("40000000-0000-0000-0000-000000000002");
        private static readonly Guid MedicineId = Guid.Parse(
            "40000000-0000-0000-0000-000000000003"
        );
        private static readonly DateTimeOffset Start = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        private static readonly DateTimeOffset End = new(2026, 2, 1, 0, 0, 0, TimeSpan.Zero);

        [Fact]
        public void PatientIdsJson_RoundTripsListOfGuids()
        {
            var ids = new[] { Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7() };
            var analysis = new ClinicalAnalysis { PatientIdsJson = JsonSerializer.Serialize(ids) };

            var deserialized = JsonSerializer.Deserialize<List<Guid>>(analysis.PatientIdsJson);

            Assert.NotNull(deserialized);
            Assert.Equal(3, deserialized.Count);
            Assert.Equal(ids, deserialized);
        }

        [Fact]
        public void PatientIdsJson_EmptyArrayDeserialisesToEmptyList()
        {
            var analysis = new ClinicalAnalysis { PatientIdsJson = "[]" };

            var deserialized = JsonSerializer.Deserialize<List<Guid>>(analysis.PatientIdsJson);

            Assert.NotNull(deserialized);
            Assert.Empty(deserialized);
        }

        [Fact]
        public void TreatmentPlanIdsJson_RoundTripsListOfGuids()
        {
            var ids = new[] { Guid.CreateVersion7(), Guid.CreateVersion7() };
            var analysis = new ClinicalAnalysis
            {
                TreatmentPlanIdsJson = JsonSerializer.Serialize(ids),
            };

            var deserialized = JsonSerializer.Deserialize<List<Guid>>(analysis.TreatmentPlanIdsJson);

            Assert.NotNull(deserialized);
            Assert.Equal(2, deserialized.Count);
            Assert.Equal(ids, deserialized);
        }

        [Fact]
        public void TreatmentPlanIdsJson_EmptyArrayDeserialisesToEmptyList()
        {
            var analysis = new ClinicalAnalysis { TreatmentPlanIdsJson = "[]" };

            var deserialized = JsonSerializer.Deserialize<List<Guid>>(analysis.TreatmentPlanIdsJson);

            Assert.NotNull(deserialized);
            Assert.Empty(deserialized);
        }

        [Fact]
        public void ReportJson_RoundTripsThroughJsonDocument()
        {
            var analysis = new ClinicalAnalysis
            {
                ReportJson = BuildReportJson(AnalysisId, DoctorId, Start, End),
            };

            using var document = JsonDocument.Parse(analysis.ReportJson);
            var root = document.RootElement;

            Assert.Equal(JsonValueKind.Object, root.ValueKind);
            Assert.Equal(AnalysisId, root.GetProperty("analysisId").GetGuid());

            // The payload survives a parse/serialize cycle without losing data
            var serialized = JsonSerializer.Serialize(root);
            using var reparsed = JsonDocument.Parse(serialized);

            Assert.Equal(
                root.GetProperty("analysisId").GetGuid(),
                reparsed.RootElement.GetProperty("analysisId").GetGuid()
            );
            Assert.Equal(
                root.GetProperty("totalPatients").GetInt32(),
                reparsed.RootElement.GetProperty("totalPatients").GetInt32()
            );
        }

        [Fact]
        public void ReportJson_EmptyObjectDeserialisesWithoutThrowing()
        {
            var analysis = new ClinicalAnalysis { ReportJson = "{}" };

            using var document = JsonSerializer.Deserialize<JsonDocument>(analysis.ReportJson);

            Assert.NotNull(document);
            Assert.Equal(JsonValueKind.Object, document!.RootElement.ValueKind);
        }

        [Fact]
        public void MalformedReportJson_ThrowsJsonException()
        {
            var analysis = new ClinicalAnalysis { ReportJson = "{ this is not json" };

            Assert.Throws<JsonException>(() =>
                JsonSerializer.Deserialize<JsonDocument>(analysis.ReportJson)
            );
        }

        [Fact]
        public void ReportJson_PreservesAnalysisIdDoctorIdStartAndEnd()
        {
            var analysis = new ClinicalAnalysis
            {
                ReportJson = BuildReportJson(AnalysisId, DoctorId, Start, End),
            };

            using var document = JsonDocument.Parse(analysis.ReportJson);
            var root = document.RootElement;

            Assert.Equal(AnalysisId, root.GetProperty("analysisId").GetGuid());
            Assert.Equal(DoctorId, root.GetProperty("doctorId").GetGuid());
            Assert.Equal(Start, root.GetProperty("start").GetDateTimeOffset());
            Assert.Equal(End, root.GetProperty("end").GetDateTimeOffset());
        }

        [Fact]
        public void ReportJson_PreservesCounters()
        {
            var analysis = new ClinicalAnalysis
            {
                ReportJson = BuildReportJson(AnalysisId, DoctorId, Start, End),
            };

            using var document = JsonDocument.Parse(analysis.ReportJson);
            var root = document.RootElement;

            Assert.Equal(4, root.GetProperty("totalTreatmentPlans").GetInt32());
            Assert.Equal(3, root.GetProperty("totalPatients").GetInt32());
            Assert.Equal(2, root.GetProperty("totalAdherentPlans").GetInt32());
            Assert.Equal(1, root.GetProperty("totalNonAdherentPlans").GetInt32());
            Assert.Equal(1, root.GetProperty("totalPendingReviewPlans").GetInt32());
            Assert.Equal(1, root.GetProperty("totalDeceasedPatients").GetInt32());
            Assert.Equal(2, root.GetProperty("totalRecoveredPatients").GetInt32());
            Assert.Equal(3, root.GetProperty("totalOtherOutcomePatients").GetInt32());
        }

        [Fact]
        public void ReportJson_PreservesPerDoctorBreakdown()
        {
            var analysis = new ClinicalAnalysis
            {
                ReportJson = BuildReportJson(AnalysisId, DoctorId, Start, End),
            };

            using var document = JsonDocument.Parse(analysis.ReportJson);
            var doctors = document.RootElement.GetProperty("doctors");

            Assert.Equal(JsonValueKind.Array, doctors.ValueKind);
            Assert.Equal(2, doctors.GetArrayLength());

            var first = doctors[0];
            Assert.Equal(DoctorId, first.GetProperty("doctorId").GetGuid());
            Assert.Equal(2, first.GetProperty("totalPatients").GetInt32());
            Assert.Equal(2, first.GetProperty("totalTreatmentPlans").GetInt32());
            Assert.Equal(1, first.GetProperty("totalAdherentPlans").GetInt32());
            Assert.Equal(1, first.GetProperty("totalNonAdherentPlans").GetInt32());
            Assert.Equal(0, first.GetProperty("totalPendingReviewPlans").GetInt32());

            var second = doctors[1];
            Assert.Equal(
                Guid.Parse("40000000-0000-0000-0000-000000000004"),
                second.GetProperty("doctorId").GetGuid()
            );
            Assert.Equal(1, second.GetProperty("totalPatients").GetInt32());
            Assert.Equal(2, second.GetProperty("totalTreatmentPlans").GetInt32());
        }

        [Fact]
        public void ReportJson_PreservesPerMedicineBreakdown()
        {
            var analysis = new ClinicalAnalysis
            {
                ReportJson = BuildReportJson(AnalysisId, DoctorId, Start, End),
            };

            using var document = JsonDocument.Parse(analysis.ReportJson);
            var medicines = document.RootElement
                .GetProperty("doctors")[0]
                .GetProperty("medicines");

            Assert.Equal(JsonValueKind.Array, medicines.ValueKind);
            Assert.Equal(2, medicines.GetArrayLength());

            var first = medicines[0];
            Assert.Equal(MedicineId, first.GetProperty("medicineId").GetGuid());
            Assert.Equal("Amoxicillin", first.GetProperty("name").GetString());
            Assert.Equal("Penicillin", first.GetProperty("classification").GetString());
            Assert.Equal(2, first.GetProperty("totalTreatmentPlans").GetInt32());
            Assert.Equal(1, first.GetProperty("totalAdherentPlans").GetInt32());
            Assert.Equal(1, first.GetProperty("totalNonAdherentPlans").GetInt32());
            Assert.Equal(0, first.GetProperty("totalPendingReviewPlans").GetInt32());

            var second = medicines[1];
            Assert.Equal(
                Guid.Parse("40000000-0000-0000-0000-000000000005"),
                second.GetProperty("medicineId").GetGuid()
            );
            Assert.Equal("Azithromycin", second.GetProperty("name").GetString());
            Assert.Equal("Macrolide", second.GetProperty("classification").GetString());
        }

        [Fact]
        public void ReportJson_UsesCamelCasePropertyNames()
        {
            var analysis = new ClinicalAnalysis
            {
                ReportJson = BuildReportJson(AnalysisId, DoctorId, Start, End),
            };

            using var document = JsonDocument.Parse(analysis.ReportJson);
            var root = document.RootElement;

            Assert.True(root.TryGetProperty("analysisId", out _));
            Assert.False(root.TryGetProperty("AnalysisId", out _));
            Assert.True(root.TryGetProperty("totalPatients", out _));
            Assert.False(root.TryGetProperty("TotalPatients", out _));
            Assert.True(root.TryGetProperty("doctors", out _));
            Assert.False(root.TryGetProperty("Doctors", out _));

            var serialized = JsonSerializer.Serialize(
                root,
                new JsonSerializerOptions(JsonSerializerDefaults.Web)
            );
            Assert.Contains("\"analysisId\"", serialized);
            Assert.DoesNotContain("\"AnalysisId\"", serialized);
        }

        private static string BuildReportJson(
            Guid analysisId,
            Guid? doctorId,
            DateTimeOffset start,
            DateTimeOffset end
        )
        {
            var doctorIdValue = doctorId is null ? "null" : $"\"{doctorId.Value:D}\"";
            var secondDoctorId = Guid.Parse("40000000-0000-0000-0000-000000000004");
            var secondMedicineId = Guid.Parse("40000000-0000-0000-0000-000000000005");

            var doctors =
                $$"""
                [
                  {
                    "doctorId": "{{DoctorId:D}}",
                    "totalPatients": 2,
                    "totalTreatmentPlans": 2,
                    "totalAdherentPlans": 1,
                    "totalNonAdherentPlans": 1,
                    "totalPendingReviewPlans": 0,
                    "medicines": [
                      {
                        "medicineId": "{{MedicineId:D}}",
                        "name": "Amoxicillin",
                        "classification": "Penicillin",
                        "totalTreatmentPlans": 2,
                        "totalAdherentPlans": 1,
                        "totalNonAdherentPlans": 1,
                        "totalPendingReviewPlans": 0
                      },
                      {
                        "medicineId": "{{secondMedicineId:D}}",
                        "name": "Azithromycin",
                        "classification": "Macrolide",
                        "totalTreatmentPlans": 1,
                        "totalAdherentPlans": 0,
                        "totalNonAdherentPlans": 1,
                        "totalPendingReviewPlans": 0
                      }
                    ]
                  },
                  {
                    "doctorId": "{{secondDoctorId:D}}",
                    "totalPatients": 1,
                    "totalTreatmentPlans": 2,
                    "totalAdherentPlans": 1,
                    "totalNonAdherentPlans": 0,
                    "totalPendingReviewPlans": 1,
                    "medicines": []
                  }
                ]
                """;

            return $$"""
                {
                  "analysisId": "{{analysisId:D}}",
                  "doctorId": {{doctorIdValue}},
                  "start": "{{start.ToString("O")}}",
                  "end": "{{end.ToString("O")}}",
                  "totalTreatmentPlans": 4,
                  "totalPatients": 3,
                  "totalAdherentPlans": 2,
                  "totalNonAdherentPlans": 1,
                  "totalPendingReviewPlans": 1,
                  "totalDeceasedPatients": 1,
                  "totalRecoveredPatients": 2,
                  "totalOtherOutcomePatients": 3,
                  "doctors": {{doctors}}
                }
                """;
        }
    }
}
