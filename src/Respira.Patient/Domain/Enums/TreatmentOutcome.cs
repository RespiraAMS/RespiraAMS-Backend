using System.Text.Json.Serialization;

namespace Respira.Patient.Domain.Enums
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum TreatmentOutcome
    {
        InTreatment,
        Recovered,
        Death,
    }
}
