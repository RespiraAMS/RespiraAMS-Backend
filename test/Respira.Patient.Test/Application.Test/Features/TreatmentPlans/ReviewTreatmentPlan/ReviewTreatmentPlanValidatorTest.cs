using Respira.Patient.Application.Features.TreatmentPlans.ReviewTreatmentPlan;
using Respira.Patient.Domain.Enums;

namespace Respira.Patient.Application.Test.Features.TreatmentPlans.ReviewTreatmentPlan
{
    public class ReviewTreatmentPlanValidatorTest
    {
        private readonly ReviewTreatmentPlanValidator _validator = new();

        #region Valid command

        // The review command only accepts the two decisions the doctor can make
        [Theory]
        [InlineData(AdherenceState.NotAdhereValid)]
        [InlineData(AdherenceState.NotAdhereInvalid)]
        public async Task ReviewTreatmentPlan_ValidReviewState_Success(AdherenceState reviewState)
        {
            var command = CreateCommand(Guid.CreateVersion7(), reviewState);

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
        public async Task ReviewTreatmentPlan_ValidPlanId_Success(Guid treatmentPlanId)
        {
            var command = CreateCommand(treatmentPlanId, AdherenceState.NotAdhereValid);

            var result = await _validator.ValidateAsync(
                command, TestContext.Current.CancellationToken);

            Assert.Empty(result.Errors);
        }

        #endregion

        #region Invalid command

        [Fact]
        public async Task ReviewTreatmentPlan_EmptyPlanId_Fail()
        {
            // Boundary: Guid.Empty is the only GUID value rejected by NotEmpty
            var command = CreateCommand(Guid.Empty, AdherenceState.NotAdhereValid);

            var result = await _validator.ValidateAsync(
                command, TestContext.Current.CancellationToken);

            _ = Assert.Single(result.Errors);
            Assert.Equal(
                nameof(ReviewTreatmentPlanCommand.TreatmentPlanId),
                result.Errors[0].PropertyName);
        }

        /*
         * Business rule: a review can only resolve a non-adherent plan to Valid or
         * Invalid. Adhere is engine-owned, NotAdhere is an intermediate state, and
         * values outside the defined enum are not decisions at all.
         */
        [Theory]
        [InlineData(AdherenceState.Adhere)]
        [InlineData(AdherenceState.NotAdhere)]
        [InlineData((AdherenceState)4)]
        [InlineData((AdherenceState)(-1))]
        [InlineData((AdherenceState)int.MaxValue)]
        public async Task ReviewTreatmentPlan_NonReviewState_Fail(AdherenceState reviewState)
        {
            var command = CreateCommand(Guid.CreateVersion7(), reviewState);

            var result = await _validator.ValidateAsync(
                command, TestContext.Current.CancellationToken);

            _ = Assert.Single(result.Errors);
            Assert.Equal(
                nameof(ReviewTreatmentPlanCommand.AdherenceState),
                result.Errors[0].PropertyName);
        }

        [Fact]
        public async Task ReviewTreatmentPlan_AllSectionsInvalid_Fail()
        {
            var command = CreateCommand(Guid.Empty, AdherenceState.Adhere);

            var result = await _validator.ValidateAsync(
                command, TestContext.Current.CancellationToken);

            Assert.Equal(2, result.Errors.Count);
            Assert.Contains(result.Errors,
                x => x.PropertyName == nameof(ReviewTreatmentPlanCommand.TreatmentPlanId));
            Assert.Contains(result.Errors,
                x => x.PropertyName == nameof(ReviewTreatmentPlanCommand.AdherenceState));
        }

        #endregion

        private static ReviewTreatmentPlanCommand CreateCommand(
            Guid treatmentPlanId,
            AdherenceState adherenceState)
        {
            return new ReviewTreatmentPlanCommand
            {
                TreatmentPlanId = treatmentPlanId,
                AdherenceState = adherenceState,
            };
        }
    }
}
