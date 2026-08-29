using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Domain.Enums;

/// <summary>
/// This enum is the patient status on the whole treatment process
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PatientStatus
{
    [Display(Name = "In treatment")] InTreatment,
    [Display(Name = "Recovered")] Recovered,
    [Display(Name = "Death")] Death
}
