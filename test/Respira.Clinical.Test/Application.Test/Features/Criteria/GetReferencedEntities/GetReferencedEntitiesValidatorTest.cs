using Respira.Clinical.Application.Features.Criteria.GetReferencedEntities;

namespace Respira.Application.Test.Features.Criteria.GetReferencedEntities
{
    public class GetReferencedEntitiesValidatorTest
    {
        private readonly GetReferencedEntitiesValidator _validator = new();

        #region Valid id

        public static readonly TheoryData<Guid> ValidIds =
        [
            Guid.CreateVersion7(),
            Guid.NewGuid(),
            new Guid("ffffffff-ffff-ffff-ffff-ffffffffffff"),
        ];

        [Theory]
        [MemberData(nameof(ValidIds))]
        public async Task GetReferencedEntities_Success(Guid id)
        {
            var result = await _validator.ValidateAsync(
                new GetReferencedEntitiesQuery { Id = id },
                TestContext.Current.CancellationToken);

            Assert.Empty(result.Errors);
        }

        #endregion

        #region Invalid id

        [Fact]
        public async Task GetReferencedEntities_EmptyId_Fail()
        {
            var result = await _validator.ValidateAsync(
                new GetReferencedEntitiesQuery { Id = Guid.Empty },
                TestContext.Current.CancellationToken);

            _ = Assert.Single(result.Errors);
            Assert.Equal("Id", result.Errors[0].PropertyName);
        }

        #endregion
    }
}
