namespace Domain.Enums;

/// <summary>
/// Antibiotic routes of administration
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum RouteOfAdministration
{
    [Display(Name = "Oral")] [Description("Consume through mouth")]
    Oral,

    [Display(Name = "Intravenous")] [Description("Consume through vein")]
    Intravenous,
}