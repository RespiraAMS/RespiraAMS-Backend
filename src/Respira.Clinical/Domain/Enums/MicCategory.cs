using System.Text.Json.Serialization;

namespace Respira.Clinical.Domain.Enums
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum MicCategory
    {
        Susceptible,
        Intermediate,
        Resistant,
    }
}
