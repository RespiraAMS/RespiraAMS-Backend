using Respira.Clinical.Application.Features.Antibiograms.DeleteAntibiogram;

namespace Respira.Clinical.Application.Test.Features.Antibiograms.DeleteAntibiogram
{
    public class DeleteAntibiogramValidatorTest
    {
        private readonly DeleteAntibiogramValidator _validator = new();

        #region Valid command

        // Boundary GUIDs: NotEmpty accepts every value except Guid.Empty
        public static readonly TheoryData<Guid> NonEmptyIds =
        [
            Guid.CreateVersion7(),
            new Guid("00000000-0000-0000-0000-000000000001"),
            new Guid("ffffffff-ffff-ffff-ffff-ffffffffffff"),
        ];

        [Theory]
        [MemberData(nameof(NonEmptyIds))]
        public async Task DeleteAntibiogram_ValidId_Success(Guid id)
        {
            var result = await _validator.ValidateAsync(
                new DeleteAntibiogramCommand { Id = id },
                TestContext.Current.CancellationToken);

            Assert.Empty(result.Errors);
        }

        #endregion

        #region Invalid command

        [Fact]
        public async Task DeleteAntibiogram_EmptyId_Fail()
        {
            // Boundary: Guid.Empty can never identify a stored antibiogram
            var result = await _validator.ValidateAsync(
                new DeleteAntibiogramCommand { Id = Guid.Empty },
                TestContext.Current.CancellationToken);

            _ = Assert.Single(result.Errors);
            Assert.Equal("Id", result.Errors[0].PropertyName);
        }

        #endregion
    }
}
