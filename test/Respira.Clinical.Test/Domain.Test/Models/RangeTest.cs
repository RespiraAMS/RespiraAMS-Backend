namespace Domain.Test.Models;

using Range = Domain.Models.Range;
public class RangeTest
{
    #region IsInRange

    public static TheoryData<Range, decimal, bool> HappyPath_IsInRange =
        [
#pragma warning disable xUnit1047 // Avoid using TheoryDataRow arguments that might not be serializable

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
#pragma warning restore xUnit1047 // Avoid using TheoryDataRow arguments that might not be serializable
        ];

    [Theory]
#pragma warning disable xUnit1045 // Avoid using TheoryData type arguments that might not be serializable
    [MemberData(nameof(HappyPath_IsInRange))]
#pragma warning restore xUnit1045 // Avoid using TheoryData type arguments that might not be serializable
    public void IsInRange_Success(Range range, decimal value, bool expected)
    {
        Assert.Equal(range.IsInRange(value), expected);
    }

    #endregion

    #region IsRangeOverlapped

    public static TheoryData<Range, Range, bool> HappyPath_IsRangeOverlapped =
        [
#pragma warning disable xUnit1047 // Avoid using TheoryDataRow arguments that might not be serializable

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
#pragma warning restore xUnit1047 // Avoid using TheoryDataRow arguments that might not be serializable
        ];

    [Theory]
#pragma warning disable xUnit1045 // Avoid using TheoryData type arguments that might not be serializable
    [MemberData(nameof(HappyPath_IsRangeOverlapped))]
#pragma warning restore xUnit1045 // Avoid using TheoryData type arguments that might not be serializable
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
}
