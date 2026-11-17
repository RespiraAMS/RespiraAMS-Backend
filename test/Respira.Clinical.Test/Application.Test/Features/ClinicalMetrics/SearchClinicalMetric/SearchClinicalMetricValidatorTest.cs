using Respira.Clinical.Application.Features.ClinicalMetrics.SearchClinicalMetric;
using Respira.ServiceDefaults.Contracts.Pagination;

namespace Respira.Clinical.Application.Test.Features.ClinicalMetrics.SearchClinicalMetric
{
    public class SearchClinicalMetricValidatorTest
    {
        private readonly SearchClinicalMetricValidator _validator = new();

        #region Valid command

        // IsValidSearchTerm boundaries: the trimmed length must be >= 2 and every
        // character must be alphanumeric, a hyphen or whitespace (so multi word and
        // padded terms are accepted)
        [Theory]
        [InlineData("ab")]              // lower boundary of the trimmed length
        [InlineData("4c")]              // digits are accepted
        [InlineData("curb")]            // realistic single word
        [InlineData("curb-65")]         // hyphen is allowed
        [InlineData("blood urea")]      // realistic multi word term
        [InlineData("  ab  ")]          // padding is allowed and ignored by the length rule
        [InlineData("IdsA2026")]        // mixed letters and digits
        [InlineData("Kháng")]           // accented Unicode letters count as letters
        public async Task SearchClinicalMetric_ValidSearchTerm_Success(string searchTerm)
        {
            var result = await _validator.ValidateAsync(new SearchClinicalMetricQuery
            {
                Param = new PaginationParam { Page = 1, Size = 10 },
                SearchTerm = searchTerm,
            }, TestContext.Current.CancellationToken);

            Assert.Empty(result.Errors);
        }

        // IsValidPaginationParam boundaries: page >= 1 and 1 <= size <= 100
        public static readonly TheoryData<int, int> ValidParams =
        [
            (1, 1),   // smallest allowed values
            (1, 10),  // default values
            (1, 100), // largest allowed size
            (7, 50),
        ];

        [Theory]
        [MemberData(nameof(ValidParams))]
        public async Task SearchClinicalMetric_ValidPaginationParam_Success(int page, int size)
        {
            var result = await _validator.ValidateAsync(new SearchClinicalMetricQuery
            {
                Param = new PaginationParam { Page = page, Size = size },
                SearchTerm = "curb",
            }, TestContext.Current.CancellationToken);

            Assert.Empty(result.Errors);
        }

        #endregion

        #region Invalid search term - length

        // Fails only the trimmed-length rule (>= 2); whitespace and single letters
        // still satisfy the character-set rule, so exactly one error is reported
        [Theory]
        [InlineData("")]      // empty
        [InlineData("a")]     // one character, just below the length boundary
        [InlineData(" ")]     // whitespace only
        [InlineData("   ")]   // whitespace only, longer
        [InlineData("  a  ")] // one character surrounded by padding
        public async Task SearchClinicalMetric_SearchTermTooShort_Fail(string searchTerm)
        {
            var result = await _validator.ValidateAsync(new SearchClinicalMetricQuery
            {
                Param = new PaginationParam { Page = 1, Size = 10 },
                SearchTerm = searchTerm,
            }, TestContext.Current.CancellationToken);

            _ = Assert.Single(result.Errors);
            Assert.Equal("SearchTerm", result.Errors[0].PropertyName);
        }

        #endregion

        #region Invalid search term - character set

        // Fails only the character-set rule: the trimmed length is >= 2 but the term
        // contains at least one character that is neither alphanumeric, a hyphen nor
        // whitespace
        [Theory]
        [InlineData("curb&65")]      // ampersand
        [InlineData("50%")]          // percent
        [InlineData("covid-19!")]    // exclamation mark (the hyphen is allowed)
        [InlineData("idsa/ats")]     // slash
        [InlineData("ab_cd")]        // underscore
        public async Task SearchClinicalMetric_SearchTermWithForbiddenCharacter_Fail(string searchTerm)
        {
            var result = await _validator.ValidateAsync(new SearchClinicalMetricQuery
            {
                Param = new PaginationParam { Page = 1, Size = 10 },
                SearchTerm = searchTerm,
            }, TestContext.Current.CancellationToken);

            _ = Assert.Single(result.Errors);
            Assert.Equal("SearchTerm", result.Errors[0].PropertyName);
        }

        #endregion

        #region Invalid search term - length and character set

        // Fails both rules at once, so SearchTerm carries two errors
        [Theory]
        [InlineData("!")]   // single forbidden character, too short as well
        [InlineData("%")]   // single forbidden character, too short as well
        [InlineData("&")]   // single forbidden character, too short as well
        [InlineData(" @ ")] // one forbidden character surrounded by padding
        public async Task SearchClinicalMetric_SearchTermTooShortAndForbiddenCharacter_Fail(string searchTerm)
        {
            var result = await _validator.ValidateAsync(new SearchClinicalMetricQuery
            {
                Param = new PaginationParam { Page = 1, Size = 10 },
                SearchTerm = searchTerm,
            }, TestContext.Current.CancellationToken);

            Assert.Equal(2, result.Errors.Count);
            Assert.All(result.Errors, x => Assert.Equal("SearchTerm", x.PropertyName));
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
        public async Task SearchClinicalMetric_InvalidPaginationParam_Fail(int page, int size)
        {
            var result = await _validator.ValidateAsync(new SearchClinicalMetricQuery
            {
                Param = new PaginationParam { Page = page, Size = size },
                SearchTerm = "curb",
            }, TestContext.Current.CancellationToken);

            _ = Assert.Single(result.Errors);
            Assert.Equal("Param", result.Errors[0].PropertyName);
        }

        #endregion

        #region Invalid search term and pagination param

        [Fact]
        public async Task SearchClinicalMetric_InvalidSearchTermAndPaginationParam_Fail()
        {
            var result = await _validator.ValidateAsync(new SearchClinicalMetricQuery
            {
                Param = new PaginationParam { Page = 0, Size = 10 },
                SearchTerm = "idsa/ats",
            }, TestContext.Current.CancellationToken);

            Assert.Equal(2, result.Errors.Count);
            Assert.Contains(result.Errors, x => x.PropertyName == "SearchTerm");
            Assert.Contains(result.Errors, x => x.PropertyName == "Param");
        }

        #endregion
    }
}
