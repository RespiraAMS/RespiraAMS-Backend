using System.Text.Json.Serialization;

namespace Respira.Clinical.Domain.Enums
{
    /// <summary>
    /// Minimum Inhibitory Concentration.
    /// Note that, MIC usually measure in numeric value (ug/mL),
    /// and this is just the category of the MIC value range
    /// (since the system scope doesn't required that level of detail)
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum MicCategory
    {
        Susceptible,
        Intermediate,
        Resistant,
    }
}
