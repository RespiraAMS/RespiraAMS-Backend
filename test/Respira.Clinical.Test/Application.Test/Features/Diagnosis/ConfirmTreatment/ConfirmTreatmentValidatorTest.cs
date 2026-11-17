using System.Text.Json;
using Respira.Clinical.Application.Features.Diagnosis.ConfirmTreatment;

namespace Respira.Clinical.Application.Test.Features.Diagnosis.ConfirmTreatment
{
    public class ConfirmTreatmentValidatorTest
    {
        private readonly ConfirmTreatmentValidator _validator = new();

        #region Valid query

        [Fact]
        public async Task ConfirmTreatment_MinimalValues_Success()
        {
            // Boundary: exactly 1 medicine is the smallest accepted medicine list,
            // and a diagnosis payload with no protocol is still a valid JSON object
            var query = CreateQuery([Guid.CreateVersion7()]);

            var result = await _validator.ValidateAsync(
                query, TestContext.Current.CancellationToken);

            Assert.Empty(result.Errors);
        }

        [Fact]
        public async Task ConfirmTreatment_MultipleMedicinesWithReason_Success()
        {
            // Conventional moderate CAP therapy: 3rd generation cephalosporin + macrolide
            var query = CreateQuery(
                [Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7()],
                "Patient is allergic to the recommended macrolide, switched to a respiratory fluoroquinolone");

            var result = await _validator.ValidateAsync(
                query, TestContext.Current.CancellationToken);

            Assert.Empty(result.Errors);
        }

        [Fact]
        public async Task ConfirmTreatment_ReasonIsOptional_Success()
        {
            // Boundary: a null reason is accepted, the adherence rule is enforced in the handler
            var query = CreateQuery([Guid.CreateVersion7()], reason: null);

            var result = await _validator.ValidateAsync(
                query, TestContext.Current.CancellationToken);

            Assert.Empty(result.Errors);
        }

        [Fact]
        public async Task ConfirmTreatment_FullDiagnosisPayload_Success()
        {
            /*
             * The validator only checks that the diagnosis is a JSON object. The full
             * DiagnoseResult snapshot (unknown to ConfirmDiagnosisView) must not make
             * the request invalid.
             */
            var query = CreateQuery([Guid.CreateVersion7()]);
            query.Diagnosis = Parse(
                $$"""
                {
                  "diagnosisId": "{{Guid.CreateVersion7()}}",
                  "severity": "Severe",
                  "treatmentSite": "IntensiveCareUnit",
                  "egfr": 48.5,
                  "isPatientPregnantOrInLactationPhase": false,
                  "protocols": [
                    {
                      "id": "{{Guid.CreateVersion7()}}",
                      "medicines": [[{ "id": "{{Guid.CreateVersion7()}}" }]]
                    }
                  ]
                }
                """);

            var result = await _validator.ValidateAsync(
                query, TestContext.Current.CancellationToken);

            Assert.Empty(result.Errors);
        }

        #endregion

        #region Invalid query

        [Fact]
        public async Task ConfirmTreatment_EmptyDoctorId_Fail()
        {
            // Boundary: Guid.Empty is the only UUID value rejected by the rule
            var query = CreateQuery([Guid.CreateVersion7()]);
            query.DoctorId = Guid.Empty;

            var result = await _validator.ValidateAsync(
                query, TestContext.Current.CancellationToken);

            _ = Assert.Single(result.Errors);
            Assert.Equal("DoctorId", result.Errors[0].PropertyName);
        }

        [Fact]
        public async Task ConfirmTreatment_EmptyPatientId_Fail()
        {
            // Boundary: Guid.Empty is the only UUID value rejected by the rule
            var query = CreateQuery([Guid.CreateVersion7()]);
            query.PatientId = Guid.Empty;

            var result = await _validator.ValidateAsync(
                query, TestContext.Current.CancellationToken);

            _ = Assert.Single(result.Errors);
            Assert.Equal("PatientId", result.Errors[0].PropertyName);
        }

        [Fact]
        public async Task ConfirmTreatment_DefaultDiagnosis_Fail()
        {
            // Boundary: a missing JSON payload is ValueKind.Undefined, not an object
            var query = CreateQuery([Guid.CreateVersion7()]);
            query.Diagnosis = default;

            var result = await _validator.ValidateAsync(
                query, TestContext.Current.CancellationToken);

            _ = Assert.Single(result.Errors);
            Assert.Equal("Diagnosis", result.Errors[0].PropertyName);
        }

        // Every JSON kind other than an object must be rejected
        [Theory]
        [InlineData("[]")]
        [InlineData("[{\"diagnosisId\":\"0192f5c0-0000-7000-8000-000000000001\"}]")]
        [InlineData("\"Severe\"")]
        [InlineData("42")]
        [InlineData("true")]
        [InlineData("null")]
        public async Task ConfirmTreatment_NonObjectDiagnosis_Fail(string diagnosisJson)
        {
            var query = CreateQuery([Guid.CreateVersion7()]);
            query.Diagnosis = Parse(diagnosisJson);

            var result = await _validator.ValidateAsync(
                query, TestContext.Current.CancellationToken);

            _ = Assert.Single(result.Errors);
            Assert.Equal("Diagnosis", result.Errors[0].PropertyName);
        }

        [Fact]
        public async Task ConfirmTreatment_EmptyMedicineList_Fail()
        {
            // Boundary: 0 medicines is the lower boundary of the medicine list
            var query = CreateQuery([]);

            var result = await _validator.ValidateAsync(
                query, TestContext.Current.CancellationToken);

            _ = Assert.Single(result.Errors);
            Assert.Equal("Medicines", result.Errors[0].PropertyName);
        }

        [Fact]
        public async Task ConfirmTreatment_DuplicateMedicine_Fail()
        {
            // Boundary: 2 entries with the same ID is the smallest duplicated list
            var id = Guid.CreateVersion7();
            var query = CreateQuery([id, id]);

            var result = await _validator.ValidateAsync(
                query, TestContext.Current.CancellationToken);

            _ = Assert.Single(result.Errors);
            Assert.Equal("Medicines", result.Errors[0].PropertyName);
        }

        [Fact]
        public async Task ConfirmTreatment_EmptyMedicineId_Fail()
        {
            // Boundary: Guid.Empty is the only UUID value rejected by the per-item rule
            var query = CreateQuery([Guid.CreateVersion7(), Guid.Empty]);

            var result = await _validator.ValidateAsync(
                query, TestContext.Current.CancellationToken);

            _ = Assert.Single(result.Errors);
            Assert.Equal("Medicines[1]", result.Errors[0].PropertyName);
        }

        [Fact]
        public async Task ConfirmTreatment_EmptyMedicineListWithEmptyId_Fail()
        {
            // A single empty ID: the list is not empty (so NotEmpty passes) but the item is invalid
            var query = CreateQuery([Guid.Empty]);

            var result = await _validator.ValidateAsync(
                query, TestContext.Current.CancellationToken);

            _ = Assert.Single(result.Errors);
            Assert.Equal("Medicines[0]", result.Errors[0].PropertyName);
        }

        /*
         * The query is deserialized from an HTTP body, so an explicit JSON null for
         * the medicine list is representable (the required modifier is a compile-time
         * contract, System.Text.Json only rejects a *missing* member). Validation
         * must report an error instead of throwing.
         */
        [Fact]
        public async Task ConfirmTreatment_NullMedicineList_Fail()
        {
            var query = CreateQuery([Guid.CreateVersion7()]);
            query.Medicines = null!;

            var result = await _validator.ValidateAsync(
                query, TestContext.Current.CancellationToken);

            Assert.Contains(result.Errors, x => x.PropertyName == "Medicines");
        }

        [Fact]
        public async Task ConfirmTreatment_AllSectionsInvalid_Fail()
        {
            var query = new ConfirmTreatmentQuery
            {
                DoctorId = Guid.Empty,
                PatientId = Guid.Empty,
                Diagnosis = Parse("[]"),
                Medicines = [Guid.Empty, Guid.Empty],
                ReasonForMedicineChoice = null,
            };

            var result = await _validator.ValidateAsync(
                query, TestContext.Current.CancellationToken);

            Assert.Contains(result.Errors, x => x.PropertyName == "DoctorId");
            Assert.Contains(result.Errors, x => x.PropertyName == "PatientId");
            Assert.Contains(result.Errors, x => x.PropertyName == "Diagnosis");
            Assert.Contains(result.Errors, x => x.PropertyName == "Medicines");
            Assert.Contains(result.Errors, x => x.PropertyName == "Medicines[0]");
            Assert.Contains(result.Errors, x => x.PropertyName == "Medicines[1]");
        }

        #endregion

        private static ConfirmTreatmentQuery CreateQuery(
            List<Guid> medicines,
            string? reason = null)
        {
            return new ConfirmTreatmentQuery
            {
                DoctorId = Guid.CreateVersion7(),
                PatientId = Guid.CreateVersion7(),
                Diagnosis = Parse(
                    $$"""
                    {
                      "diagnosisId": "{{Guid.CreateVersion7()}}",
                      "protocols": [
                        {
                          "medicines": [[{ "id": "{{Guid.CreateVersion7()}}" }]]
                        }
                      ]
                    }
                    """),
                Medicines = medicines,
                ReasonForMedicineChoice = reason,
            };
        }

        private static JsonElement Parse(string json)
        {
            // Clone so the element stays valid after the document is disposed
            return JsonDocument.Parse(json).RootElement.Clone();
        }
    }
}
