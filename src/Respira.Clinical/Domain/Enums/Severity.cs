namespace Domain.Enums;

/// <summary>
/// Disease severity
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum Severity
{
    [Display(Name = "Mild")] Mild,
    [Display(Name = "Moderate")] Moderate,
    [Display(Name = "Severe")] Severe
}