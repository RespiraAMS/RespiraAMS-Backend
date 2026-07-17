namespace Domain.Enums;

/// <summary>
/// Where to receive treatment. It can also be used to deduce the severity of the patient
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TreatmentSite
{
    [Display(Name = "Outpatient")] Outpatient,
    [Display(Name = "Inpatient")] Inpatient,

    [Display(Name = "Intensive Care Unit (ICU)")]
    IntensiveCareUnit,
}