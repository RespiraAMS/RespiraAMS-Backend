namespace Domain.Enums;

/// <summary>
/// The MIC (Minimum Inhibitory Concentration) is the lowest concentration of an antibiotic
/// that prevents visible bacterial growth. This enum represent establishes interpretive breakpoints
/// (in μg/mL or mg/L) for this scale, using The Clinical and Laboratory Standards Institute (CLSI) standard
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum MinimumInhibitoryConcentration
{
    [Display(Name = "Susceptible")] Susceptible,
    [Display(Name = "Intermediate")] Intermediate,
    [Display(Name = "Resistance")] Resistance
}