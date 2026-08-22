using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Domain.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TreatmentType
{
    [Display(Name = "Empirical Therapy")] EmpiricalTherapy,
    [Display(Name = "Targeted Therapy")] TargetedTherapy
}