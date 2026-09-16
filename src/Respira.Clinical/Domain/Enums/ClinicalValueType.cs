using System.Text.Json.Serialization;

namespace Respira.Domain.Enums
{
    /// <summary>
    /// Clinical variable value type
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum ClinicalValueType
    {
        Numeric,
        Boolean
    }
}
