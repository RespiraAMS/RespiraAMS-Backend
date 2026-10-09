using Respira.Clinical.Application.Features.RiskFactors.CreateRiskFactor;

namespace Respira.Application.Test.Features.RiskFactors.CreateRiskFactor
{
    public class CreateRiskFactorValidatorTest
    {
        private readonly CreateRiskFactorValidator _validator = new();

        #region Valid command

        public static readonly TheoryData<Guid, Guid> ValidCommands =
        [
            (Guid.CreateVersion7(), Guid.CreateVersion7()),
            (Guid.CreateVersion7(), Guid.CreateVersion7()),
            // Upper boundary of the GUID range: every bit set is still a valid id
            (
                new Guid("ffffffff-ffff-ffff-ffff-ffffffffffff"),
                new Guid("ffffffff-ffff-ffff-ffff-ffffffffffff")),
        ];

        [Theory]
        [MemberData(nameof(ValidCommands))]
        public async Task CreateRiskFactor_Success(Guid pathogenId, Guid criterionId)
        {
            var result = await _validator.ValidateAsync(new CreateRiskFactorCommand
            {
                PathogenId = pathogenId,
                CriterionId = criterionId,
            }, TestContext.Current.CancellationToken);

            Assert.Empty(result.Errors);
        }

        #endregion

        #region Invalid command

        public static readonly TheoryData<Guid, Guid, string> InvalidCommands =
        [
            // Boundary: empty GUID violates NotEmpty on PathogenId
            (Guid.Empty, Guid.CreateVersion7(), "PathogenId"),
            // Boundary: empty GUID violates NotEmpty on CriterionId
            (Guid.CreateVersion7(), Guid.Empty, "CriterionId"),
        ];

        [Theory]
        [MemberData(nameof(InvalidCommands))]
        public async Task CreateRiskFactor_Fail(Guid pathogenId, Guid criterionId, string property)
        {
            var result = await _validator.ValidateAsync(new CreateRiskFactorCommand
            {
                PathogenId = pathogenId,
                CriterionId = criterionId,
            }, TestContext.Current.CancellationToken);

            _ = Assert.Single(result.Errors);
            Assert.Equal(property, result.Errors[0].PropertyName);
        }

        [Fact]
        public async Task CreateRiskFactor_BothIdsEmpty_Fail()
        {
            // Both rules fire independently, so the client learns about both mistakes
            var result = await _validator.ValidateAsync(new CreateRiskFactorCommand
            {
                PathogenId = Guid.Empty,
                CriterionId = Guid.Empty,
            }, TestContext.Current.CancellationToken);

            Assert.Equal(2, result.Errors.Count);
            Assert.Contains(result.Errors, x => x.PropertyName == "PathogenId");
            Assert.Contains(result.Errors, x => x.PropertyName == "CriterionId");
        }

        #endregion
    }
}
