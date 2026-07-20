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
    /// Boolean flag: true is &lt;=, false is &lt;
    /// </summary>
    public required bool IsMinExclusive { get; set; }

    /// <summary>
    /// Max value
    /// </summary>
    public required decimal Max { get; set; }

    /// <summary>
    /// Boolean flag: true is &gt;=, false is &gt;
    /// </summary>
    public required bool IsMaxExclusive { get; set; }

    /// <summary>
    /// Numeric unit (null means no unit) 
    /// </summary>
    public required string? Unit { get; set; }

    public bool IsInRange(decimal value)
    {
        var result = Min < value && value < Max;
        if (IsMinExclusive)
        {
            result = result || value == Min;
        }
        if (IsMaxExclusive)
        {
            result = result || value == Max;
        }
        return result;
    }
}