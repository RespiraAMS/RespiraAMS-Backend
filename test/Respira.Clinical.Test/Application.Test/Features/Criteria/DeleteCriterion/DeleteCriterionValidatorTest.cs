using Respira.Clinical.Application.Features.Criteria.DeleteCriterion;

namespace Respira.Application.Test.Features.Criteria.DeleteCriterion
{
    public class DeleteCriterionValidatorTest
    {
        private readonly DeleteCriterionValidator _validator = new();

        #region Valid command

        // Boundary: a UUID v7 is the usual shape, plus the lowest and highest
        // non-empty GUID values that NotEmpty still accepts
        public static readonly TheoryData<Guid> ValidIds =
        [
            Guid.CreateVersion7(),
            new Guid("00000000-0000-0000-0000-000000000001"),
            new Guid("ffffffff-ffff-ffff-ffff-ffffffffffff"),
        ];

        [Theory]
        [MemberData(nameof(ValidIds))]
        public async Task DeleteCriterion_ValidId_Success(Guid id)
        {
            var result = await _validator.ValidateAsync(
                new DeleteCriterionCommand { Id = id },
                TestContext.Current.CancellationToken);

            Assert.Empty(result.Errors);
        }

        #endregion

        #region Invalid command

        [Fact]
        public async Task DeleteCriterion_EmptyId_Fail()
        {
            // Boundary: Guid.Empty is the only value rejected by NotEmpty on a Guid
            var result = await _validator.ValidateAsync(
                new DeleteCriterionCommand { Id = Guid.Empty },
                TestContext.Current.CancellationToken);

            _ = Assert.Single(result.Errors);
            Assert.Equal("Id", result.Errors[0].PropertyName);
        }

        #endregion
    }
}
