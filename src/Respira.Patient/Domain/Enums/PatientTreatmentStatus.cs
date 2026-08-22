using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Domain.Enums;

/// <summary>
/// This enum represent the patient status in each treatment.
/// When creating a new treatment, we just assume that it's a favorable response,
/// until it's not :v. Creating a middle state like progress does not make sense,
/// since you only want to do something when the patient has poor response
/// -> FavorableResponse will never have a chance to do anything
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PatientTreatmentStatus
{
    [Display(Name = "Favorable response")] FavorableResponse,
    [Display(Name = "Poor response")] PoorResponse
}
