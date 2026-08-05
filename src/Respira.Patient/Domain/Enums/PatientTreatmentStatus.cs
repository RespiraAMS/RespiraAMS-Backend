using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Domain.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PatientTreatmentStatus
{
    [Display(Name = "Favorable response")] FavorableResponse,
    [Display(Name = "Poor response")] PoorResponse
}