using Respira.Patient.Application.Features.TreatmentPlans.CloseTreatmentPlan;

namespace Respira.Patient.Application.Test.Features.TreatmentPlans.CloseTreatmentPlan
{
    public class CloseTreatmentPlanValidatorTest
    {
        private readonly CloseTreatmentPlanValidator _validator = new();

        #region Valid command

        // A realistic close reason, plus a short one to prove there is no minimum
        // length beyond NotEmpty
        [Theory]
        [InlineData("Patient completed the antibiotic course and was discharged in stable condition")]
        [InlineData("Recovered")]
        public async Task CloseTreatmentPlan_ValidReason_Success(string reason)
        {
            var command = CreateCommand(Guid.CreateVersion7(), reason);

            var result = await _validator.ValidateAsync(
                command, TestContext.Current.CancellationToken);

            Assert.Empty(result.Errors);
        }

        // Boundary: a UUID v7 is the usual shape, plus the lowest and highest
        // non-empty GUID values that NotEmpty still accepts
        public static readonly TheoryData<Guid> ValidPlanIds =
        [
            Guid.CreateVersion7(),
            new Guid("00000000-0000-0000-0000-000000000001"),
            new Guid("ffffffff-ffff-ffff-ffff-ffffffffffff"),
        ];

        [Theory]
        [MemberData(nameof(ValidPlanIds))]
        public async Task CloseTreatmentPlan_ValidPlanId_Success(Guid treatmentPlanId)
        {
            var command = CreateCommand(
                treatmentPlanId,
                "Patient completed the antibiotic course");

            var result = await _validator.ValidateAsync(
                command, TestContext.Current.CancellationToken);

            Assert.Empty(result.Errors);
        }

        #endregion

        #region Invalid command

        [Fact]
        public async Task CloseTreatmentPlan_EmptyPlanId_Fail()
        {
            // Boundary: Guid.Empty is the only GUID value rejected by NotEmpty
            var command = CreateCommand(
                Guid.Empty,
                "Patient completed the antibiotic course");

            var result = await _validator.ValidateAsync(
                command, TestContext.Current.CancellationToken);

            _ = Assert.Single(result.Errors);
            Assert.Equal(
                nameof(CloseTreatmentPlanCommand.TreatmentPlanId),
                result.Errors[0].PropertyName);
        }

        // Boundary: null, empty and whitespace-only are the values NotEmpty rejects
        // for the required close reason
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData("\t")]
        [InlineData("\n")]
        public async Task CloseTreatmentPlan_MissingReason_Fail(string? reason)
        {
            var command = CreateCommand(Guid.CreateVersion7(), reason!);

            var result = await _validator.ValidateAsync(
                command, TestContext.Current.CancellationToken);

            _ = Assert.Single(result.Errors);
            Assert.Equal(
                nameof(CloseTreatmentPlanCommand.Reason),
                result.Errors[0].PropertyName);
        }

        [Fact]
        public async Task CloseTreatmentPlan_AllSectionsInvalid_Fail()
        {
            var command = CreateCommand(Guid.Empty, "");

            var result = await _validator.ValidateAsync(
                command, TestContext.Current.CancellationToken);

            Assert.Equal(2, result.Errors.Count);
            Assert.Contains(result.Errors,
                x => x.PropertyName == nameof(CloseTreatmentPlanCommand.TreatmentPlanId));
            Assert.Contains(result.Errors,
                x => x.PropertyName == nameof(CloseTreatmentPlanCommand.Reason));
        }

        #endregion

        private static CloseTreatmentPlanCommand CreateCommand(
            Guid treatmentPlanId,
            string reason)
        {
            return new CloseTreatmentPlanCommand
            {
                TreatmentPlanId = treatmentPlanId,
                Reason = reason,
            };
        }
    }
}
