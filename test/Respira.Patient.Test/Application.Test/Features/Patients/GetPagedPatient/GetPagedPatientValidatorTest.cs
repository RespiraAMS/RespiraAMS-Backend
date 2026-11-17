using Respira.Patient.Application.Features.Patients.GetPagedPatient;
using Respira.ServiceDefaults.Contracts.Pagination;

namespace Respira.Patient.Application.Test.Features.Patients.GetPagedPatient
{
    public class GetPagedPatientValidatorTest
    {
        private readonly GetPagedPatientValidator _validator = new();

        #region Valid command

        [Fact]
        public async Task GetPagedPatient_RealisticQuery_Success()
        {
            // The default ward listing: first page of ten patients, no filter
            var result = await _validator.ValidateAsync(
                ValidQuery(), TestContext.Current.CancellationToken);

            Assert.Empty(result.Errors);
        }

        // Boundary of the pagination rules: Page must be > 0 and Size must be
        // within (0, 100] - smallest page/size, the maximum size of 100, and the
        // fact that Page has no upper bound (an out-of-range deep page is legal
        // and simply returns an empty page)
        [Theory]
        [InlineData(1, 1)]
        [InlineData(1, 100)]
        [InlineData(999, 10)]
        public async Task GetPagedPatient_ValidPagination_Success(int page, int size)
        {
            var result = await _validator.ValidateAsync(
                ValidQuery() with { Param = new PaginationParam { Page = page, Size = size } },
                TestContext.Current.CancellationToken);

            Assert.Empty(result.Errors);
        }

        // Boundary of "min <= max when both are set": equal bounds are allowed
        [Fact]
        public async Task GetPagedPatient_MinAgeEqualsMaxAge_Success()
        {
            var result = await _validator.ValidateAsync(
                ValidQuery() with { Filter = new PatientFilter { MinAge = 34, MaxAge = 34 } },
                TestContext.Current.CancellationToken);

            Assert.Empty(result.Errors);
        }

        // The range rule only applies when BOTH bounds are present: a lone lower
        // bound (or a lone upper bound) can never violate min <= max
        [Fact]
        public async Task GetPagedPatient_OnlyOneAgeBoundSet_Success()
        {
            var result = await _validator.ValidateAsync(
                ValidQuery() with { Filter = new PatientFilter { MinAge = 90 } },
                TestContext.Current.CancellationToken);

            Assert.Empty(result.Errors);
        }

        [Fact]
        public async Task GetPagedPatient_NullFilter_Success()
        {
            // Without a filter the age-range rule has nothing to check
            var result = await _validator.ValidateAsync(
                ValidQuery() with { Filter = null }, TestContext.Current.CancellationToken);

            Assert.Empty(result.Errors);
        }

        #endregion

        #region Invalid command

        // Boundary of "Page > 0": 0 itself is rejected, the rule is exclusive
        [Fact]
        public async Task GetPagedPatient_PageZero_Fail()
        {
            var result = await _validator.ValidateAsync(
                ValidQuery() with { Param = new PaginationParam { Page = 0, Size = 10 } },
                TestContext.Current.CancellationToken);

            _ = Assert.Single(result.Errors);
            Assert.Equal("Param", result.Errors[0].PropertyName);
        }

        // Boundary of Size (0, 100]: both edges of the range fail - 0 is the
        // exclusive lower edge, 101 the first value above the maximum page size
        [Theory]
        [InlineData(0)]
        [InlineData(101)]
        public async Task GetPagedPatient_InvalidSize_Fail(int size)
        {
            var result = await _validator.ValidateAsync(
                ValidQuery() with { Param = new PaginationParam { Page = 1, Size = size } },
                TestContext.Current.CancellationToken);

            _ = Assert.Single(result.Errors);
            Assert.Equal("Param", result.Errors[0].PropertyName);
        }

        // Boundary of "min <= max": MinAge one year above MaxAge is the smallest
        // possible violation of the range rule
        [Fact]
        public async Task GetPagedPatient_MinAgeGreaterThanMaxAge_Fail()
        {
            var result = await _validator.ValidateAsync(
                ValidQuery() with { Filter = new PatientFilter { MinAge = 35, MaxAge = 34 } },
                TestContext.Current.CancellationToken);

            _ = Assert.Single(result.Errors);
            Assert.Equal("Filter", result.Errors[0].PropertyName);
        }

        [Fact]
        public async Task GetPagedPatient_AllFieldsInvalid_Fail()
        {
            // Boundary: every rule that can fail at once fails independently, so the
            // client learns about all mistakes in a single response
            var query = new GetPagedPatientQuery
            {
                Param = new PaginationParam { Page = 0, Size = 101 }, // both edges broken
                Filter = new PatientFilter { MinAge = 70, MaxAge = 16 }, // reversed range
            };

            var result = await _validator.ValidateAsync(query, TestContext.Current.CancellationToken);

            Assert.Contains(result.Errors, x => x.PropertyName == "Param");
            Assert.Contains(result.Errors, x => x.PropertyName == "Filter");
        }

        #endregion

        private static GetPagedPatientQuery ValidQuery() => new()
        {
            Param = new PaginationParam { Page = 1, Size = 10 },
        };
    }
}
