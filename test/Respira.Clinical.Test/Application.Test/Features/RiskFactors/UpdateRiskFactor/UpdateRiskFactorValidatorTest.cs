using Respira.Clinical.Application.Features.RiskFactors.UpdateRiskFactor;

namespace Respira.Application.Test.Features.RiskFactors.UpdateRiskFactor
{
    public class UpdateRiskFactorValidatorTest
    {
        private readonly UpdateRiskFactorValidator _validator = new();

        #region Valid command

        // Boundary: a UUID v7 is the usual shape, plus the lowest and highest
        // non-empty GUID values that NotEmpty still accepts
        public static readonly TheoryData<Guid, Guid, Guid> ValidCommands =
        [
            (Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7()),
            (
                new Guid("00000000-0000-0000-0000-000000000001"),
                new Guid("00000000-0000-0000-0000-000000000001"),
                new Guid("00000000-0000-0000-0000-000000000001")),
            (
                new Guid("ffffffff-ffff-ffff-ffff-ffffffffffff"),
                new Guid("ffffffff-ffff-ffff-ffff-ffffffffffff"),
                new Guid("ffffffff-ffff-ffff-ffff-ffffffffffff")),
        ];

        [Theory]
        [MemberData(nameof(ValidCommands))]
        public async Task UpdateRiskFactor_Success(Guid id, Guid pathogenId, Guid criterionId)
        {
            var result = await _validator.ValidateAsync(new UpdateRiskFactorCommand
            {
                Id = id,
                PathogenId = pathogenId,
                CriterionId = criterionId,
            }, TestContext.Current.CancellationToken);

            Assert.Empty(result.Errors);
        }

        #endregion

        #region Invalid command

        public static readonly TheoryData<Guid, Guid, Guid, string> InvalidCommands =
        [
            // Boundary: Guid.Empty is the only value rejected by NotEmpty on a Guid
            (Guid.Empty, Guid.CreateVersion7(), Guid.CreateVersion7(), "Id"),
            (Guid.CreateVersion7(), Guid.Empty, Guid.CreateVersion7(), "PathogenId"),
            (Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.Empty, "CriterionId"),
        ];

        [Theory]
        [MemberData(nameof(InvalidCommands))]
        public async Task UpdateRiskFactor_Fail(
            Guid id, Guid pathogenId, Guid criterionId, string property)
        {
            var result = await _validator.ValidateAsync(new UpdateRiskFactorCommand
            {
                Id = id,
                PathogenId = pathogenId,
                CriterionId = criterionId,
            }, TestContext.Current.CancellationToken);

            _ = Assert.Single(result.Errors);
            Assert.Equal(property, result.Errors[0].PropertyName);
        }

        [Fact]
        public async Task UpdateRiskFactor_AllIdsEmpty_Fail()
        {
            // Every rule of the validator fires at once, so the client learns about
            // all three mistakes in one round trip
            var result = await _validator.ValidateAsync(new UpdateRiskFactorCommand
            {
                Id = Guid.Empty,
                PathogenId = Guid.Empty,
                CriterionId = Guid.Empty,
            }, TestContext.Current.CancellationToken);

            Assert.Equal(3, result.Errors.Count);
            Assert.Contains(result.Errors, x => x.PropertyName == "Id");
            Assert.Contains(result.Errors, x => x.PropertyName == "PathogenId");
            Assert.Contains(result.Errors, x => x.PropertyName == "CriterionId");
        }

        #endregion
    }
}
