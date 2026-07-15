namespace Domain.Enums;

/*
 * AWaRe metrics is categorized into 3 main class:
 * 1. Access
 * 2. Watch
 * 3. Reserve
 * Some medicines can be both in Access and Watch, thus another category `Access-Watch`
 * Some medicines is often used even though they are not classify by WHO, so they are `Others`
 * In some rare cases that some medicines that didn't have any classification,
 * then `Unclassified`
 */

/// <summary>
/// Antibiotic categorized metric by WHO
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AwareCategory
{
    [Display(Name = "Access")] Access,
    [Display(Name = "Access-Watch")] AccessWatch,
    [Display(Name = "Watch")] Watch,
    [Display(Name = "Reserve")] Reserve,
    [Display(Name = "Others")] Others,
    [Display(Name = "Unclassified")] Unclassified,
}