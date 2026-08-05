namespace Domain.Enums;

/// <summary>
/// Criterion type
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CriterionType
{
    Boolean,
    Numeric
}