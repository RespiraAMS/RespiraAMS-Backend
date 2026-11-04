using Respira.Patient.Application.Features.TreatmentPlans.UpsertTreatmentPlan;
using Respira.Patient.Domain.Models;

namespace Respira.Patient.Application.Test.Features.TreatmentPlans.UpsertTreatmentPlan
{
    public class UpsertTreatmentPlanValidatorTest
    {
        private readonly UpsertTreatmentPlanValidator _validator = new();

        #region Valid command

        [Fact]
        public async Task UpsertTreatmentPlan_AdhereWithoutReason_Success()
        {
            // Boundary: 1 medicine is the smallest accepted treatment plan, and an
            // adherent pick must not carry a reason
            var command = CreateCommand(isAdhere: true, reasonForMedicineChoice: null);

            var result = await _validator.ValidateAsync(
                command, TestContext.Current.CancellationToken);

            Assert.Empty(result.Errors);
        }

        [Fact]
        public async Task UpsertTreatmentPlan_NotAdhereWithReason_Success()
        {
            // Boundary: 3 medicines is the largest realistic custom combination
            var command = CreateCommand(
                isAdhere: false,
                reasonForMedicineChoice:
                    "Blood culture grew Pseudomonas aeruginosa; escalated to meropenem and added vancomycin for suspected MRSA",
                medicines: ThreeMedicineCombination());

            var result = await _validator.ValidateAsync(
                command, TestContext.Current.CancellationToken);

            Assert.Empty(result.Errors);
        }

        [Fact]
        public async Task UpsertTreatmentPlan_NotAdhereWithPaddedReason_Success()
        {
            // Boundary: the reason rule trims before checking, so surrounding
            // whitespace does not turn a real justification into an empty one
            var command = CreateCommand(
                isAdhere: false,
                reasonForMedicineChoice: "  Culture confirmed Klebsiella pneumoniae  ");

            var result = await _validator.ValidateAsync(
                command, TestContext.Current.CancellationToken);

            Assert.Empty(result.Errors);
        }

        [Fact]
        public async Task UpsertTreatmentPlan_TwoMedicines_Success()
        {
            // Conventional moderate CAP therapy: third-generation cephalosporin
            // plus a macrolide
            var command = CreateCommand(medicines: TwoMedicineCombination());

            var result = await _validator.ValidateAsync(
                command, TestContext.Current.CancellationToken);

            Assert.Empty(result.Errors);
        }

        #endregion

        #region Invalid command

        // Boundary: Guid.Empty is the only GUID value rejected by NotEmpty
        [Theory]
        [InlineData(nameof(UpsertTreatmentPlanCommand.DoctorId))]
        [InlineData(nameof(UpsertTreatmentPlanCommand.PatientId))]
        [InlineData(nameof(UpsertTreatmentPlanCommand.DiagnosisId))]
        public async Task UpsertTreatmentPlan_EmptyRequiredId_Fail(string propertyName)
        {
            var command = CreateCommand();
            switch (propertyName)
            {
                case nameof(UpsertTreatmentPlanCommand.DoctorId):
                    command.DoctorId = Guid.Empty;
                    break;
                case nameof(UpsertTreatmentPlanCommand.PatientId):
                    command.PatientId = Guid.Empty;
                    break;
                case nameof(UpsertTreatmentPlanCommand.DiagnosisId):
                    command.DiagnosisId = Guid.Empty;
                    break;
            }

            var result = await _validator.ValidateAsync(
                command, TestContext.Current.CancellationToken);

            _ = Assert.Single(result.Errors);
            Assert.Equal(propertyName, result.Errors[0].PropertyName);
        }

        // Boundary: null, empty and whitespace-only are the three values NotEmpty
        // rejects for a required string
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData("\t")]
        public async Task UpsertTreatmentPlan_MissingDiagnosis_Fail(string? diagnosis)
        {
            var command = CreateCommand();
            command.Diagnosis = diagnosis!;

            var result = await _validator.ValidateAsync(
                command, TestContext.Current.CancellationToken);

            _ = Assert.Single(result.Errors);
            Assert.Equal(nameof(UpsertTreatmentPlanCommand.Diagnosis), result.Errors[0].PropertyName);
        }

        // Boundary: 0 medicines is the lower boundary of the medicine list
        [Fact]
        public async Task UpsertTreatmentPlan_EmptyMedicineList_Fail()
        {
            var command = CreateCommand(medicines: []);

            var result = await _validator.ValidateAsync(
                command, TestContext.Current.CancellationToken);

            _ = Assert.Single(result.Errors);
            Assert.Equal(nameof(UpsertTreatmentPlanCommand.Medicines), result.Errors[0].PropertyName);
        }

        [Fact]
        public async Task UpsertTreatmentPlan_NullMedicineList_Fail()
        {
            var command = CreateCommand();
            command.Medicines = null!;

            var result = await _validator.ValidateAsync(
                command, TestContext.Current.CancellationToken);

            Assert.Contains(result.Errors,
                x => x.PropertyName == nameof(UpsertTreatmentPlanCommand.Medicines));
        }

        [Fact]
        public async Task UpsertTreatmentPlan_EmptyMedicineId_Fail()
        {
            // Boundary: Guid.Empty is the only antibiotic ID rejected by NotEmpty
            var command = CreateCommand(medicines:
            [
                new Antibiotic(Guid.Empty, "Ceftriaxone", "Watch"),
            ]);

            var result = await _validator.ValidateAsync(
                command, TestContext.Current.CancellationToken);

            _ = Assert.Single(result.Errors);
            Assert.Equal("Medicines[0].Id", result.Errors[0].PropertyName);
        }

        // Boundary: null, empty and whitespace-only are rejected for a required
        // string member of a medicine
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData("\t")]
        public async Task UpsertTreatmentPlan_MissingMedicineName_Fail(string? name)
        {
            var command = CreateCommand(medicines:
            [
                new Antibiotic(Guid.CreateVersion7(), name!, "Watch"),
            ]);

            var result = await _validator.ValidateAsync(
                command, TestContext.Current.CancellationToken);

            _ = Assert.Single(result.Errors);
            Assert.Equal("Medicines[0].Name", result.Errors[0].PropertyName);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData("\t")]
        public async Task UpsertTreatmentPlan_MissingMedicineClassification_Fail(string? classification)
        {
            var command = CreateCommand(medicines:
            [
                new Antibiotic(Guid.CreateVersion7(), "Ceftriaxone", classification!),
            ]);

            var result = await _validator.ValidateAsync(
                command, TestContext.Current.CancellationToken);

            _ = Assert.Single(result.Errors);
            Assert.Equal("Medicines[0].Classification", result.Errors[0].PropertyName);
        }

        // Invalid medicine members are reported per item, using the item index
        [Fact]
        public async Task UpsertTreatmentPlan_InvalidLaterMedicine_Fail()
        {
            var command = CreateCommand(medicines:
            [
                new Antibiotic(Guid.CreateVersion7(), "Ceftriaxone", "Watch"),
                new Antibiotic(Guid.Empty, "", ""),
            ]);

            var result = await _validator.ValidateAsync(
                command, TestContext.Current.CancellationToken);

            Assert.Equal(3, result.Errors.Count);
            Assert.All(result.Errors, x => Assert.StartsWith("Medicines[1]", x.PropertyName));
        }

        /*
         * Business rule: the reason must be null exactly when the doctor adheres to
         * the recommended protocol. Any non-null value is rejected for an adherent
         * pick, including an empty string and whitespace.
         */
        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData("\t")]
        [InlineData("Patient requested a different medicine")]
        public async Task UpsertTreatmentPlan_AdhereWithReason_Fail(string? reason)
        {
            var command = CreateCommand(isAdhere: true, reasonForMedicineChoice: reason);

            var result = await _validator.ValidateAsync(
                command, TestContext.Current.CancellationToken);

            _ = Assert.Single(result.Errors);
            Assert.Equal(string.Empty, result.Errors[0].PropertyName);
        }

        /*
         * Business rule: a non-adherent pick must explain why. A null, empty or
         * whitespace-only reason is not an explanation.
         */
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData("\t")]
        [InlineData("\n")]
        public async Task UpsertTreatmentPlan_NotAdhereWithoutReason_Fail(string? reason)
        {
            var command = CreateCommand(isAdhere: false, reasonForMedicineChoice: reason);

            var result = await _validator.ValidateAsync(
                command, TestContext.Current.CancellationToken);

            _ = Assert.Single(result.Errors);
            Assert.Equal(string.Empty, result.Errors[0].PropertyName);
        }

        [Fact]
        public async Task UpsertTreatmentPlan_AllSectionsInvalid_Fail()
        {
            var command = new UpsertTreatmentPlanCommand
            {
                DoctorId = Guid.Empty,
                PatientId = Guid.Empty,
                DiagnosisId = Guid.Empty,
                Diagnosis = "",
                Medicines =
                [
                    new Antibiotic(Guid.Empty, "", ""),
                ],
                ReasonForMedicineChoice = null,
                IsAdhere = false,
            };

            var result = await _validator.ValidateAsync(
                command, TestContext.Current.CancellationToken);

            Assert.Contains(result.Errors,
                x => x.PropertyName == nameof(UpsertTreatmentPlanCommand.DoctorId));
            Assert.Contains(result.Errors,
                x => x.PropertyName == nameof(UpsertTreatmentPlanCommand.PatientId));
            Assert.Contains(result.Errors,
                x => x.PropertyName == nameof(UpsertTreatmentPlanCommand.DiagnosisId));
            Assert.Contains(result.Errors,
                x => x.PropertyName == nameof(UpsertTreatmentPlanCommand.Diagnosis));
            Assert.Contains(result.Errors, x => x.PropertyName == "Medicines[0].Id");
            Assert.Contains(result.Errors, x => x.PropertyName == "Medicines[0].Name");
            Assert.Contains(result.Errors, x => x.PropertyName == "Medicines[0].Classification");
            Assert.Contains(result.Errors, x => x.PropertyName?.Length == 0);
        }

        #endregion

        private static UpsertTreatmentPlanCommand CreateCommand(
            bool isAdhere = true,
            string? reasonForMedicineChoice = null,
            List<Antibiotic>? medicines = null)
        {
            var diagnosisId = Guid.CreateVersion7();
            return new UpsertTreatmentPlanCommand
            {
                DoctorId = Guid.CreateVersion7(),
                PatientId = Guid.CreateVersion7(),
                DiagnosisId = diagnosisId,
                Diagnosis = $$"""
                    {
                      "diagnosisId": "{{diagnosisId}}",
                      "severity": "Severe",
                      "treatmentSite": "Inpatient",
                      "egfr": 48.5,
                      "isPatientPregnantOrInLactationPhase": false
                    }
                    """,
                Medicines = medicines ??
                [
                    new Antibiotic(Guid.CreateVersion7(), "Ceftriaxone", "Watch"),
                ],
                ReasonForMedicineChoice = reasonForMedicineChoice,
                IsAdhere = isAdhere,
            };
        }

        private static List<Antibiotic> TwoMedicineCombination()
        {
            // Conventional moderate CAP therapy: third-generation cephalosporin
            // plus a macrolide
            return
            [
                new Antibiotic(Guid.CreateVersion7(), "Ceftriaxone", "Watch"),
                new Antibiotic(Guid.CreateVersion7(), "Azithromycin", "Watch"),
            ];
        }

        private static List<Antibiotic> ThreeMedicineCombination()
        {
            return
            [
                new Antibiotic(Guid.CreateVersion7(), "Meropenem", "Reserve"),
                new Antibiotic(Guid.CreateVersion7(), "Azithromycin", "Watch"),
                new Antibiotic(Guid.CreateVersion7(), "Vancomycin", "Watch"),
            ];
        }
    }
}
