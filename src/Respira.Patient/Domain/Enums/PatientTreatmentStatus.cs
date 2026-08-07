using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Domain.Enums;

/// <summary>
/// This enum represent the patient status in each treatment
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PatientTreatmentStatus
{
    [Display(Name = "Favorable response")] FavorableResponse,
    [Display(Name = "Poor response")] PoorResponse
}