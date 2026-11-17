using Respira.Analysis.Domain.Entities;
using Respira.ServiceDefaults.Models;

namespace Respira.Analysis.Domain.Test.Entities
{
    public class ClinicalAnalysisTest
    {
        [Fact]
        public void NewAnalysis_HasNonEmptyUuidV7Id()
        {
            var analysis = new ClinicalAnalysis();

            Assert.NotEqual(Guid.Empty, analysis.Id);
            // Guid.CreateVersion7() stamps the RFC 4122 version nibble with 7
            Assert.Equal(7, analysis.Id.Version);
        }

        [Fact]
        public void NewAnalysis_CreatedAtAndUpdatedAtDefaultToNow()
        {
            var before = DateTimeOffset.UtcNow;

            var analysis = new ClinicalAnalysis();

            var after = DateTimeOffset.UtcNow;
            Assert.InRange(analysis.CreatedAt, before, after);
            Assert.InRange(analysis.UpdatedAt, before, after);
        }

        [Fact]
        public void NewAnalysis_IsDeletedDefaultsToFalseAndDeletedAtDefaultsToNull()
        {
            var analysis = new ClinicalAnalysis();

            Assert.False(analysis.IsDeleted);
            Assert.Null(analysis.DeletedAt);
        }

        [Fact]
        public void NewAnalysis_DoctorIdDefaultsToNull()
        {
            // A system wide report is not owned by a single doctor
            var analysis = new ClinicalAnalysis();

            Assert.Null(analysis.DoctorId);
        }

        [Fact]
        public void NewAnalysis_MedicineIdDefaultsToNull()
        {
            var analysis = new ClinicalAnalysis();

            Assert.Null(analysis.MedicineId);
        }

        [Fact]
        public void NewAnalysis_StartAndEndDefaultToDefaultDateTimeOffset()
        {
            var analysis = new ClinicalAnalysis();

            Assert.Equal(default(DateTimeOffset), analysis.Start);
            Assert.Equal(default(DateTimeOffset), analysis.End);
        }

        [Fact]
        public void NewAnalysis_PatientIdsJsonDefaultsToEmptyArray()
        {
            var analysis = new ClinicalAnalysis();

            Assert.Equal("[]", analysis.PatientIdsJson);
        }

        [Fact]
        public void NewAnalysis_TreatmentPlanIdsJsonDefaultsToEmptyArray()
        {
            var analysis = new ClinicalAnalysis();

            Assert.Equal("[]", analysis.TreatmentPlanIdsJson);
        }

        [Fact]
        public void NewAnalysis_ReportJsonDefaultsToEmptyObject()
        {
            var analysis = new ClinicalAnalysis();

            Assert.Equal("{}", analysis.ReportJson);
        }

        [Fact]
        public void Analysis_CanCarryDoctorMedicineStartAndEnd()
        {
            var doctorId = Guid.CreateVersion7();
            var medicineId = Guid.CreateVersion7();
            var start = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
            var end = new DateTimeOffset(2026, 2, 1, 0, 0, 0, TimeSpan.Zero);

            var analysis = new ClinicalAnalysis
            {
                DoctorId = doctorId,
                MedicineId = medicineId,
                Start = start,
                End = end,
            };

            Assert.Equal(doctorId, analysis.DoctorId);
            Assert.Equal(medicineId, analysis.MedicineId);
            Assert.Equal(start, analysis.Start);
            Assert.Equal(end, analysis.End);
        }

        [Fact]
        public void Analysis_JsonPayloadsCanBeOverwritten()
        {
            var analysis = new ClinicalAnalysis
            {
                PatientIdsJson = "[\"10000000-0000-0000-0000-000000000001\"]",
                TreatmentPlanIdsJson = "[\"20000000-0000-0000-0000-000000000001\"]",
                ReportJson = "{\"analysisId\":\"30000000-0000-0000-0000-000000000001\"}",
            };

            Assert.Equal("[\"10000000-0000-0000-0000-000000000001\"]", analysis.PatientIdsJson);
            Assert.Equal(
                "[\"20000000-0000-0000-0000-000000000001\"]",
                analysis.TreatmentPlanIdsJson
            );
            Assert.Equal(
                "{\"analysisId\":\"30000000-0000-0000-0000-000000000001\"}",
                analysis.ReportJson
            );
        }

        [Fact]
        public void Analysis_DerivesFromServiceDefaultsBase()
        {
            var analysis = new ClinicalAnalysis();

            Assert.IsAssignableFrom<Base>(analysis);
            Assert.IsNotType<Base>(analysis);
        }

        [Fact]
        public void Analysis_IdIsStableWhenAssignedExplicitly()
        {
            var id = Guid.Parse("70000000-0000-0000-0000-000000000001");

            var analysis = new ClinicalAnalysis { Id = id };

            Assert.Equal(id, analysis.Id);
        }

        [Fact]
        public void TwoAnalysesCreatedBackToBack_GetDifferentIds()
        {
            var first = new ClinicalAnalysis();
            var second = new ClinicalAnalysis();

            Assert.NotEqual(first.Id, second.Id);
        }
    }
}
