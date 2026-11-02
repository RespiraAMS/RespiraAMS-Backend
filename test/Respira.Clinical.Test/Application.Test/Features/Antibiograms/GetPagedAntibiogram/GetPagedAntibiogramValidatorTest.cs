using Respira.Clinical.Application.Features.Antibiograms.GetPagedAntibiogram;
using Respira.Clinical.Domain.Enums;
using Respira.ServiceDefaults.Contracts.Pagination;

namespace Respira.Clinical.Application.Test.Features.Antibiograms.GetPagedAntibiogram
{
    public class GetPagedAntibiogramValidatorTest
    {
        private readonly GetPagedAntibiogramValidator _validator = new();

        #region Valid pagination param

        // Boundaries of IsValidPaginationParam: page >= 1, 1 <= size <= 100
        public static readonly TheoryData<int, int> ValidParams =
        [
            (1, 1),   // smallest allowed values
            (1, 10),  // default size
            (1, 100), // largest allowed size
            (7, 50),
        ];

        [Theory]
        [MemberData(nameof(ValidParams))]
        public async Task GetPagedAntibiogram_ValidParam_Success(int page, int size)
        {
            var result = await _validator.ValidateAsync(new GetPagedAntibiogramQuery
            {
                Param = new PaginationParam { Page = page, Size = size },
            }, TestContext.Current.CancellationToken);

            Assert.Empty(result.Errors);
        }

        [Fact]
        public async Task GetPagedAntibiogram_WithFilters_Success()
        {
            // Filters are optional and are not validated: any pathogen id or defined
            // MIC category must pass as long as the pagination itself is valid
            var result = await _validator.ValidateAsync(new GetPagedAntibiogramQuery
            {
                Param = new PaginationParam { Page = 2, Size = 100 },
                Filter = new AntibiogramFilter
                {
                    PathogenId = Guid.CreateVersion7(),
                    MicCategory = MicCategory.Resistant,
                },
            }, TestContext.Current.CancellationToken);

            Assert.Empty(result.Errors);
        }

        #endregion

        #region Invalid pagination param

        public static readonly TheoryData<int, int> InvalidParams =
        [
            (0, 10),   // page lower boundary violation
            (-3, 10),  // negative page
            (1, 0),    // size lower boundary violation
            (1, -20),  // negative size
            (1, 101),  // size upper boundary violation
        ];

        [Theory]
        [MemberData(nameof(InvalidParams))]
        public async Task GetPagedAntibiogram_InvalidParam_Fail(int page, int size)
        {
            var result = await _validator.ValidateAsync(new GetPagedAntibiogramQuery
            {
                Param = new PaginationParam { Page = page, Size = size },
            }, TestContext.Current.CancellationToken);

            _ = Assert.Single(result.Errors);
            Assert.Equal("Param", result.Errors[0].PropertyName);
        }

        #endregion
    }
}
