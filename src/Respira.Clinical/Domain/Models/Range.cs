namespace Domain.Models;

/// <summary>
/// A custom range value, used to represent range value
/// </summary>
public class Range
{
    /// <summary>
    /// Min value
    /// </summary>
    public required decimal Min { get; set; }

    /// <summary>
    /// Boolean flag: true is &lt;, false is &lt;=
    /// </summary>
    public required bool IsMinExclusive { get; set; }

    /// <summary>
    /// Max value
    /// </summary>
    public required decimal Max { get; set; }

    /// <summary>
    /// Boolean flag: true is &gt;, false is &gt;=
    /// </summary>
    public required bool IsMaxExclusive { get; set; }

    /// <summary>
    /// Numeric unit (null means no unit)
    /// </summary>
    public required string? Unit { get; set; }

    public bool IsInRange(decimal value)
    {
        var result = Min < value && value < Max;
        if (!IsMinExclusive)
        {
            result = result || value == Min;
        }
        if (!IsMaxExclusive)
        {
            result = result || value == Max;
        }
        return result;
    }

    public bool IsRangeOverlapped(Range? range)
    {
        if (range is null) return false;

        // This range is completely to the left of the other range.
        if (Max < range.Min) return false;
        if (Max == range.Min)
        {
            return !IsMaxExclusive && !range.IsMinExclusive;
        }

        // This range is completely to the right of the other range.
        if (Min > range.Max) return false;
        if (Min == range.Max)
        {
            return !IsMinExclusive && !range.IsMaxExclusive;
        }

        return true;
    }

    public override string ToString()
    {
        return $"{(IsMinExclusive ? "(" : "[")}{Min}, {(Max == decimal.MaxValue ? "∞" : Max)}{(IsMaxExclusive ? ")" : "]")}";
    }
}
