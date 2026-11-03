using System.Text.Json.Serialization;

namespace Respira.Patient.Domain.Enums
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum AdherenceState
    {
        /// <summary>
        /// Adhere to treatment protocol
        /// </summary>
        Adhere,

        /// <summary>
        /// Not adhere to recommended treatment protocol.
        /// This is an in between state, since it need
        /// the head department doctor to review before deciding
        /// that this is valid or not
        /// </summary>
        NotAdhere,

        /// <summary>
        /// Not adhere to treatment protocol, but is acceptable
        /// </summary>
        NotAdhereValid,

        /// <summary>
        /// Not adhere to treatment protocol and is not acceptable
        /// </summary>
        NotAdhereInvalid
    }
}
