using Respira.Clinical.Application.Features.Shared.ManageRange;
using Range = Respira.Clinical.Domain.Models.Range;

namespace Respira.Application.Test.Features.Shared.ManageRange
{
    public class RangeValidatorTest
    {
        private readonly RangeValidator _validator = new();

        public static TheoryData<Range> ValidRangeData => [
            new Range { Min = 0, IsMinExclusive = false, Max = 10, IsMaxExclusive = false, Unit = "mg/dL" },
            new Range { Min = 10, IsMinExclusive = false, Max = 10, IsMaxExclusive = false, Unit = "mg/dL" },
            new Range { Min = 0, IsMinExclusive = true, Max = 10, IsMaxExclusive = false, Unit = null },
        ];

        [Theory]
        [MemberData(nameof(ValidRangeData))]
        public void Validate_ValidRange_Success(Range range)
        {
            var result = _validator.Validate(range);
            Assert.Empty(result.Errors);
        }

        [Fact]
        public void Validate_InvalidMinMaxValue_Fail()
        {
            var range = new Range
            {
                Min = 100,
                IsMinExclusive = false,
                Max = 11,
                IsMaxExclusive = true,
                Unit = "mg/dL"
            };

            var result = _validator.Validate(range);
            Assert.Single(result.Errors);
        }

        [Fact]
        public void Validate_EmptyStringUnit_Fail()
        {
            var range = new Range
            {
                Min = 0,
                IsMinExclusive = false,
                Max = 10,
                IsMaxExclusive = false,
                Unit = ""
            };

            var result = _validator.Validate(range);
            Assert.Single(result.Errors);
        }
    }
}
