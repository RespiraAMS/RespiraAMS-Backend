using Respira.Clinical.Application.Features.TreatmentProtocols.GetPagedTreatmentProtocol;
using Respira.Clinical.Domain.Enums;
using Respira.ServiceDefaults.Contracts.Pagination;

namespace Respira.Clinical.Application.Test.Features.TreatmentProtocols.GetPagedTreatmentProtocol
{
    public class GetPagedTreatmentProtocolValidatorTest
    {
        private readonly GetPagedTreatmentProtocolValidator _validator = new();

        #region Valid pagination param

        // Boundaries of IsValidPaginationParam: page >= 1 and 1 <= size <= 100.
        // The page index has no upper bound, so int.MaxValue is accepted as well
        public static readonly TheoryData<int, int> ValidParams =
        [
            (1, 1),             // smallest allowed page and size
            (1, 10),            // default size
            (1, 100),           // largest allowed size
            (7, 50),
            (int.MaxValue, 1),  // page upper boundary of the underlying int
        ];

        [Theory]
        [MemberData(nameof(ValidParams))]
        public async Task GetPagedTreatmentProtocol_ValidParam_Success(int page, int size)
        {
            var result = await _validator.ValidateAsync(new GetPagedTreatmentProtocolQuery
            {
                Param = new PaginationParam { Page = page, Size = size },
            }, TestContext.Current.CancellationToken);

            Assert.Empty(result.Errors);
        }

        [Fact]
        public async Task GetPagedTreatmentProtocol_WithFilter_Success()
        {
            // The feature only validates the pagination parameter, so an arbitrary filter
            // (including unknown ids) must not produce validation errors
            var result = await _validator.ValidateAsync(new GetPagedTreatmentProtocolQuery
            {
                Param = new PaginationParam { Page = 1, Size = 10 },
                Filter = new TreatmentProtocolFilter
                {
                    Severity = Severity.Mild,
                    TreatmentSite = TreatmentSite.Outpatient,
                    PathogenIds = [Guid.CreateVersion7()],
                    CriteriaIds = [Guid.CreateVersion7()],
                },
            }, TestContext.Current.CancellationToken);

            Assert.Empty(result.Errors);
        }

        #endregion

        #region Invalid pagination param

        public static readonly TheoryData<int, int> InvalidParams =
        [
            (0, 10),              // page lower boundary violation
            (-3, 10),             // negative page
            (int.MinValue, 10),   // page lower boundary of the underlying int
            (1, 0),               // size lower boundary violation
            (1, -20),             // negative size
            (1, 101),             // size upper boundary violation
            (1, int.MaxValue),    // size upper boundary of the underlying int
        ];

        [Theory]
        [MemberData(nameof(InvalidParams))]
        public async Task GetPagedTreatmentProtocol_InvalidParam_Fail(int page, int size)
        {
            var result = await _validator.ValidateAsync(new GetPagedTreatmentProtocolQuery
            {
                Param = new PaginationParam { Page = page, Size = size },
            }, TestContext.Current.CancellationToken);

            _ = Assert.Single(result.Errors);
            Assert.Equal("Param", result.Errors[0].PropertyName);
        }

        #endregion
    }
}
