using Domain.Enums;

namespace Domain.Models;

/// <summary>
/// Criterion used to evaluate condition, severity,...
/// </summary>
public abstract class Criterion : Base
{
    /// <summary>
    /// Criterion name
    /// </summary>
    public required string Name { get; set; }
    /// <summary>
    /// Criterion type
    /// </summary>
    public abstract CriterionType Type { get; }
}

/// <summary>
/// This criterion is a True/False type
/// </summary>
public class BooleanCriterion : Criterion
{
    public override CriterionType Type => CriterionType.Boolean;
}

/// <summary>
/// Metric-type criterion. Here, if a metric doesn't have a low/high boundary, we can use
/// the built-in min/max value to signify that it didn't have a low/high boundary.
/// For example, respiratory &gt;= 30 can be represented as Min = 30, Max = double.MaxValue  
/// </summary>
public class NumericCriterion : Criterion
{
    public override CriterionType Type => CriterionType.Numeric;
    /// <summary>
    /// Metric min threshold
    /// </summary>
    public required double Min { get; set; }
    /// <summary>
    /// Metric max threshold
    /// </summary>
    public required double Max { get; set; }
    /// <summary>
    /// Metric's unit
    /// </summary>
    public required string Unit { get; set; }
    /// <summary>
    /// Boolean flag: true if the min/max bound is exclusive
    /// </summary>
    public required bool IsExclusive { get; set; }
} 