using System.Text.Json.Serialization;

namespace Respira.Patient.Domain.Enums
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum TreatmentOutcome
    {
        InTreatment,
        Recovered,
        Death,

        /// <summary>
        /// This is used if a case turn out that they don't need antibiotic treatment
        /// </summary>
        OutOfScope
    }
}
