namespace Respira.Clinical.Domain.Test.Models
{
    using Xunit;
    using Range = Domain.Models.Range;
    public class RangeTest
    {
        #region IsInRange

        public static TheoryData<Range, decimal, bool> HappyPath_IsInRange =
        [
            // --- Closed range [10, 20]: boundaries are Min and Max, both inclusive ---
            new(new Range {Min = 10, IsMinExclusive = false, Max = 20, IsMaxExclusive = false, Unit = null}, 9.99m, false),   // just below Min
            new(new Range {Min = 10, IsMinExclusive = false, Max = 20, IsMaxExclusive = false, Unit = null}, 10m, true),     // at Min (inclusive)
            new(new Range {Min = 10, IsMinExclusive = false, Max = 20, IsMaxExclusive = false, Unit = null}, 10.01m, true),  // just above Min
            new(new Range {Min = 10, IsMinExclusive = false, Max = 20, IsMaxExclusive = false, Unit = null}, 15m, true),     // interior value
            new(new Range {Min = 10, IsMinExclusive = false, Max = 20, IsMaxExclusive = false, Unit = null}, 19.99m, true),  // just below Max
            new(new Range {Min = 10, IsMinExclusive = false, Max = 20, IsMaxExclusive = false, Unit = null}, 20m, true),     // at Max (inclusive)
            new(new Range {Min = 10, IsMinExclusive = false, Max = 20, IsMaxExclusive = false, Unit = null}, 20.01m, false), // just above Max

            // --- Open range (10, 20): both boundaries excluded ---
            new(new Range {Min = 10, IsMinExclusive = true, Max = 20, IsMaxExclusive = true, Unit = null}, 10m, false),      // at Min (exclusive)
            new(new Range {Min = 10, IsMinExclusive = true, Max = 20, IsMaxExclusive = true, Unit = null}, 10.01m, true),    // just above Min
            new(new Range {Min = 10, IsMinExclusive = true, Max = 20, IsMaxExclusive = true, Unit = null}, 19.99m, true),    // just below Max
            new(new Range {Min = 10, IsMinExclusive = true, Max = 20, IsMaxExclusive = true, Unit = null}, 20m, false),      // at Max (exclusive)

            // --- Half-open (10, 20]: Min excluded, Max included ---
            new(new Range {Min = 10, IsMinExclusive = true, Max = 20, IsMaxExclusive = false, Unit = null}, 10m, false),     // at Min (exclusive)
            new(new Range {Min = 10, IsMinExclusive = true, Max = 20, IsMaxExclusive = false, Unit = null}, 20m, true),      // at Max (inclusive)

            // --- Half-open [10, 20): Min included, Max excluded ---
            new(new Range {Min = 10, IsMinExclusive = false, Max = 20, IsMaxExclusive = true, Unit = null}, 10m, true),      // at Min (inclusive)
            new(new Range {Min = 10, IsMinExclusive = false, Max = 20, IsMaxExclusive = true, Unit = null}, 20m, false),     // at Max (exclusive)

            // --- Degenerate range, Min == Max == 10: closed single point [10, 10] ---
            new(new Range {Min = 10, IsMinExclusive = false, Max = 10, IsMaxExclusive = false, Unit = null}, 9.99m, false),  // just below the point
            new(new Range {Min = 10, IsMinExclusive = false, Max = 10, IsMaxExclusive = false, Unit = null}, 10m, true),     // exactly the point
            new(new Range {Min = 10, IsMinExclusive = false, Max = 10, IsMaxExclusive = false, Unit = null}, 10.01m, false), // just above the point

            // --- Degenerate range, both exclusive (10, 10): never satisfiable ---
            new(new Range {Min = 10, IsMinExclusive = true, Max = 10, IsMaxExclusive = true, Unit = null}, 10m, false),

            // --- Degenerate range, Min exclusive / Max inclusive (10, 10]: caught by Max check ---
            new(new Range {Min = 10, IsMinExclusive = true, Max = 10, IsMaxExclusive = false, Unit = null}, 10m, true),

            // --- Degenerate range, Min inclusive / Max exclusive [10, 10): caught by Min check ---
            new(new Range {Min = 10, IsMinExclusive = false, Max = 10, IsMaxExclusive = true, Unit = null}, 10m, true),

            // --- Negative range [-20, -10]: same boundary logic, negative numbers ---
            new(new Range {Min = -20, IsMinExclusive = false, Max = -10, IsMaxExclusive = false, Unit = null}, -20.01m, false), // just below Min
            new(new Range {Min = -20, IsMinExclusive = false, Max = -10, IsMaxExclusive = false, Unit = null}, -20m, true),    // at Min
            new(new Range {Min = -20, IsMinExclusive = false, Max = -10, IsMaxExclusive = false, Unit = null}, -10m, true),    // at Max
            new(new Range {Min = -20, IsMinExclusive = false, Max = -10, IsMaxExclusive = false, Unit = null}, -9.99m, false), // just above Max

            // --- Extreme decimal bounds, closed [decimal.MinValue, decimal.MaxValue] ---
            new(new Range {Min = decimal.MinValue, IsMinExclusive = false, Max = decimal.MaxValue, IsMaxExclusive = false, Unit = null}, decimal.MinValue, true),
            new(new Range {Min = decimal.MinValue, IsMinExclusive = false, Max = decimal.MaxValue, IsMaxExclusive = false, Unit = null}, 0m, true),
            new(new Range {Min = decimal.MinValue, IsMinExclusive = false, Max = decimal.MaxValue, IsMaxExclusive = false, Unit = null}, decimal.MaxValue, true),
        ];

        [Theory]
        [MemberData(nameof(HappyPath_IsInRange))]
        public void IsInRange_Success(Range range, decimal value, bool expected)
        {
            Assert.Equal(range.IsInRange(value), expected);
        }

        #endregion

        #region IsRangeOverlapped

        public static TheoryData<Range, Range, bool> HappyPath_IsRangeOverlapped =
        [

            // Base range used throughout: A = [10, 20] (closed)

            // --- Disjoint, with a gap ---
            new(new Range {Min = 10, IsMinExclusive = false, Max = 20, IsMaxExclusive = false, Unit = null},
                new Range {Min = 21, IsMinExclusive = false, Max = 30, IsMaxExclusive = false, Unit = null}, false), // B entirely right of A, gap
            new(new Range {Min = 10, IsMinExclusive = false, Max = 20, IsMaxExclusive = false, Unit = null},
                new Range {Min =  1, IsMinExclusive = false, Max =  5, IsMaxExclusive = false, Unit = null}, false), // B entirely left of A, gap

            // --- Touching exactly at A.Max == B.Min (right boundary) ---
            new(new Range {Min = 10, IsMinExclusive = false, Max = 20, IsMaxExclusive = false, Unit = null},
                    new Range {Min = 20, IsMinExclusive = false, Max = 30, IsMaxExclusive = false, Unit = null}, true),  // both sides closed at 20 -> touch counts as overlap
            new(new Range {Min = 10, IsMinExclusive = false, Max = 20, IsMaxExclusive = true,  Unit = null},
                    new Range {Min = 20, IsMinExclusive = false, Max = 30, IsMaxExclusive = false, Unit = null}, false), // A's Max is exclusive -> no overlap
            new(new Range {Min = 10, IsMinExclusive = false, Max = 20, IsMaxExclusive = false, Unit = null},
                    new Range {Min = 20, IsMinExclusive = true,  Max = 30, IsMaxExclusive = false, Unit = null}, false), // B's Min is exclusive -> no overlap
            new(new Range {Min = 10, IsMinExclusive = false, Max = 20, IsMaxExclusive = true,  Unit = null},
                    new Range {Min = 20, IsMinExclusive = true,  Max = 30, IsMaxExclusive = false, Unit = null}, false), // both exclusive at the touch point

            // --- Touching exactly at A.Min == B.Max (left boundary) ---
            new(new Range {Min = 10, IsMinExclusive = false, Max = 20, IsMaxExclusive = false, Unit = null},
                    new Range {Min =  1, IsMinExclusive = false, Max = 10, IsMaxExclusive = false, Unit = null}, true),  // both sides closed at 10 -> touch counts as overlap
            new(new Range {Min = 10, IsMinExclusive = true,  Max = 20, IsMaxExclusive = false, Unit = null},
                    new Range {Min =  1, IsMinExclusive = false, Max = 10, IsMaxExclusive = false, Unit = null}, false), // A's Min is exclusive -> no overlap
            new(new Range {Min = 10, IsMinExclusive = false, Max = 20, IsMaxExclusive = false, Unit = null},
                    new Range {Min =  1, IsMinExclusive = false, Max = 10, IsMaxExclusive = true,  Unit = null}, false), // B's Max is exclusive -> no overlap

            // --- Genuine overlaps (interior region, hits the final "return true") ---
            new(new Range {Min = 10, IsMinExclusive = false, Max = 20, IsMaxExclusive = false, Unit = null},
                    new Range {Min = 15, IsMinExclusive = false, Max = 25, IsMaxExclusive = false, Unit = null}, true),  // partial overlap, B shifted right
            new(new Range {Min = 10, IsMinExclusive = false, Max = 20, IsMaxExclusive = false, Unit = null},
                    new Range {Min =  5, IsMinExclusive = false, Max = 25, IsMaxExclusive = false, Unit = null}, true),  // B fully contains A
            new(new Range {Min = 10, IsMinExclusive = false, Max = 20, IsMaxExclusive = false, Unit = null},
                    new Range {Min = 12, IsMinExclusive = false, Max = 18, IsMaxExclusive = false, Unit = null}, true),  // A fully contains B
            new(new Range {Min = 10, IsMinExclusive = false, Max = 20, IsMaxExclusive = false, Unit = null},
                    new Range {Min = 10, IsMinExclusive = false, Max = 20, IsMaxExclusive = false, Unit = null}, true),  // identical ranges

            // --- Degenerate (single-point) ranges ---
            new(new Range {Min = 10, IsMinExclusive = false, Max = 10, IsMaxExclusive = false, Unit = null},
                    new Range {Min = 10, IsMinExclusive = false, Max = 10, IsMaxExclusive = false, Unit = null}, true),  // same closed point, overlaps itself
            new(new Range {Min = 10, IsMinExclusive = false, Max = 10, IsMaxExclusive = false, Unit = null},
                    new Range {Min = 10, IsMinExclusive = true,  Max = 10, IsMaxExclusive = true,  Unit = null}, false), // same point, but B's point is excluded (empty)
            new(new Range {Min = 10, IsMinExclusive = false, Max = 10, IsMaxExclusive = false, Unit = null},
                    new Range {Min = 20, IsMinExclusive = false, Max = 20, IsMaxExclusive = false, Unit = null}, false), // two different closed points, no overlap
        ];

        [Theory]
        [MemberData(nameof(HappyPath_IsRangeOverlapped))]
        public void IsRangeOverlapped_Success(Range range1, Range range2, bool expected)
        {
            Assert.Equal(range1.IsRangeOverlapped(range2), expected);
        }

        [Fact]
        public void IsRangeOverlapped_NullRange_ReturnsFalse()
        {
            var range = new Range { Min = 10, IsMinExclusive = false, Max = 20, IsMaxExclusive = false, Unit = null };
            Assert.False(range.IsRangeOverlapped(null));
        }

        #endregion

        #region IsRangeContained

        // A = range1 (inner range being checked), B = range2 (candidate container).
        // Boundary value analysis on the two comparisons: B.Min vs A.Min (lower) and B.Max vs A.Max (upper).
        public static TheoryData<Range, Range, bool> HappyPath_IsRangeContained =
        [

            // --- Lower boundary: B.Min vs A.Min (upper side kept comfortable: B.Max = 30 > A.Max = 20) ---
            new(new Range {Min = 10, IsMinExclusive = false, Max = 20, IsMaxExclusive = false, Unit = null},
                new Range {Min =  9.99m, IsMinExclusive = false, Max = 30, IsMaxExclusive = false, Unit = null}, true),  // just below A.Min -> contained
            new(new Range {Min = 10, IsMinExclusive = false, Max = 20, IsMaxExclusive = false, Unit = null},
                new Range {Min =  9.99m, IsMinExclusive = true,  Max = 30, IsMaxExclusive = false, Unit = null}, true),  // just below A.Min, B.Min exclusive -> still contained
            new(new Range {Min = 10, IsMinExclusive = false, Max = 20, IsMaxExclusive = false, Unit = null},
                new Range {Min = 10, IsMinExclusive = false, Max = 30, IsMaxExclusive = false, Unit = null}, true),     // B.Min == A.Min, both inclusive -> contained
            new(new Range {Min = 10, IsMinExclusive = true,  Max = 20, IsMaxExclusive = false, Unit = null},
                new Range {Min = 10, IsMinExclusive = false, Max = 30, IsMaxExclusive = false, Unit = null}, true),     // B.Min == A.Min, B inclusive / A exclusive -> contained
            new(new Range {Min = 10, IsMinExclusive = true,  Max = 20, IsMaxExclusive = false, Unit = null},
                new Range {Min = 10, IsMinExclusive = true,  Max = 30, IsMaxExclusive = false, Unit = null}, true),     // B.Min == A.Min, both exclusive -> contained
            new(new Range {Min = 10, IsMinExclusive = false, Max = 20, IsMaxExclusive = false, Unit = null},
                new Range {Min = 10, IsMinExclusive = true,  Max = 30, IsMaxExclusive = false, Unit = null}, false),    // B.Min == A.Min, B exclusive / A inclusive -> A.Min not in B
            new(new Range {Min = 10, IsMinExclusive = false, Max = 20, IsMaxExclusive = false, Unit = null},
                new Range {Min = 10.01m, IsMinExclusive = false, Max = 30, IsMaxExclusive = false, Unit = null}, false), // just above A.Min -> A.Min not in B
            new(new Range {Min = 10, IsMinExclusive = true,  Max = 20, IsMaxExclusive = false, Unit = null},
                new Range {Min = 10.01m, IsMinExclusive = true,  Max = 30, IsMaxExclusive = false, Unit = null}, false), // just above A.Min, both exclusive -> A has values below B.Min

            // --- Upper boundary: B.Max vs A.Max (lower side kept comfortable: B.Min = 0 < A.Min = 10) ---
            new(new Range {Min = 10, IsMinExclusive = false, Max = 20, IsMaxExclusive = false, Unit = null},
                new Range {Min =  0, IsMinExclusive = false, Max = 20.01m, IsMaxExclusive = false, Unit = null}, true),  // just above A.Max -> contained
            new(new Range {Min = 10, IsMinExclusive = false, Max = 20, IsMaxExclusive = false, Unit = null},
                new Range {Min =  0, IsMinExclusive = false, Max = 20.01m, IsMaxExclusive = true,  Unit = null}, true),  // just above A.Max, B.Max exclusive -> still contained
            new(new Range {Min = 10, IsMinExclusive = false, Max = 20, IsMaxExclusive = false, Unit = null},
                new Range {Min =  0, IsMinExclusive = false, Max = 20, IsMaxExclusive = false, Unit = null}, true),      // B.Max == A.Max, both inclusive -> contained
            new(new Range {Min = 10, IsMinExclusive = false, Max = 20, IsMaxExclusive = true,  Unit = null},
                new Range {Min =  0, IsMinExclusive = false, Max = 20, IsMaxExclusive = false, Unit = null}, true),      // B.Max == A.Max, B inclusive / A exclusive -> contained
            new(new Range {Min = 10, IsMinExclusive = false, Max = 20, IsMaxExclusive = true,  Unit = null},
                new Range {Min =  0, IsMinExclusive = false, Max = 20, IsMaxExclusive = true,  Unit = null}, true),      // B.Max == A.Max, both exclusive -> contained
            new(new Range {Min = 10, IsMinExclusive = false, Max = 20, IsMaxExclusive = false, Unit = null},
                new Range {Min =  0, IsMinExclusive = false, Max = 20, IsMaxExclusive = true,  Unit = null}, false),     // B.Max == A.Max, B exclusive / A inclusive -> A.Max not in B
            new(new Range {Min = 10, IsMinExclusive = false, Max = 20, IsMaxExclusive = false, Unit = null},
                new Range {Min =  0, IsMinExclusive = false, Max = 19.99m, IsMaxExclusive = false, Unit = null}, false), // just below A.Max -> A.Max not in B
            new(new Range {Min = 10, IsMinExclusive = false, Max = 19.99m, IsMaxExclusive = false, Unit = null},
                new Range {Min =  0, IsMinExclusive = false, Max = 19.99m, IsMaxExclusive = true,  Unit = null}, false), // B.Max == A.Max, B exclusive / A inclusive (degenerate at 19.99)

            // --- Both boundaries on the limit ---
            new(new Range {Min = 10, IsMinExclusive = false, Max = 20, IsMaxExclusive = false, Unit = null},
                new Range {Min = 10, IsMinExclusive = false, Max = 20, IsMaxExclusive = false, Unit = null}, true),      // identical closed ranges
            new(new Range {Min = 10, IsMinExclusive = true,  Max = 20, IsMaxExclusive = true,  Unit = null},
                new Range {Min = 10, IsMinExclusive = false, Max = 20, IsMaxExclusive = false, Unit = null}, true),      // open A inside closed B
            new(new Range {Min = 10, IsMinExclusive = true,  Max = 20, IsMaxExclusive = true,  Unit = null},
                new Range {Min = 10, IsMinExclusive = true,  Max = 20, IsMaxExclusive = true,  Unit = null}, true),      // identical open ranges
            new(new Range {Min = 10, IsMinExclusive = false, Max = 20, IsMaxExclusive = false, Unit = null},
                new Range {Min = 10, IsMinExclusive = true,  Max = 20, IsMaxExclusive = true,  Unit = null}, false),     // closed A inside open B -> endpoints excluded
            new(new Range {Min =  5, IsMinExclusive = false, Max = 25, IsMaxExclusive = false, Unit = null},
                new Range {Min = 10, IsMinExclusive = false, Max = 20, IsMaxExclusive = false, Unit = null}, false),     // A strictly wider than B on both sides
            new(new Range {Min = 10, IsMinExclusive = false, Max = 20, IsMaxExclusive = false, Unit = null},
                new Range {Min = 12, IsMinExclusive = false, Max = 18, IsMaxExclusive = false, Unit = null}, false),     // B strictly inside A -> not contained

            // --- Degenerate (single-point) A ---
            new(new Range {Min = 10, IsMinExclusive = false, Max = 10, IsMaxExclusive = false, Unit = null},
                new Range {Min = 10, IsMinExclusive = false, Max = 20, IsMaxExclusive = false, Unit = null}, true),      // point A inside B starting at same Min
            new(new Range {Min = 10, IsMinExclusive = false, Max = 10, IsMaxExclusive = false, Unit = null},
                new Range {Min = 10, IsMinExclusive = true,  Max = 20, IsMaxExclusive = false, Unit = null}, false),     // B excludes the point A consists of
            new(new Range {Min = 10, IsMinExclusive = true,  Max = 10, IsMaxExclusive = true,  Unit = null},
                new Range {Min = 10, IsMinExclusive = true,  Max = 20, IsMaxExclusive = false, Unit = null}, true),      // empty A is trivially contained

            // --- Negative boundaries [-20, -10] ---
            new(new Range {Min = -20, IsMinExclusive = false, Max = -10, IsMaxExclusive = false, Unit = null},
                new Range {Min = -25, IsMinExclusive = false, Max = -5, IsMaxExclusive = false, Unit = null}, true),     // B.Min just below / B.Max just above -> contained
            new(new Range {Min = -20, IsMinExclusive = false, Max = -10, IsMaxExclusive = false, Unit = null},
                new Range {Min = -20, IsMinExclusive = true,  Max = -5, IsMaxExclusive = false, Unit = null}, false),    // B.Min == A.Min, B exclusive / A inclusive
            new(new Range {Min = -20, IsMinExclusive = false, Max = -10, IsMaxExclusive = false, Unit = null},
                new Range {Min = -25, IsMinExclusive = false, Max = -10, IsMaxExclusive = true,  Unit = null}, false),   // B.Max == A.Max, B exclusive / A inclusive

            // --- Extreme decimal bounds ---
            new(new Range {Min = decimal.MinValue, IsMinExclusive = false, Max = decimal.MaxValue, IsMaxExclusive = false, Unit = null},
                new Range {Min = decimal.MinValue, IsMinExclusive = false, Max = decimal.MaxValue, IsMaxExclusive = false, Unit = null}, true),
        ];

        [Theory]
        [MemberData(nameof(HappyPath_IsRangeContained))]
        public void IsRangeContained_Success(Range range1, Range range2, bool expected)
        {
            Assert.Equal(range1.IsRangeContained(range2), expected);
        }

        #endregion
    }
}
