using System.Text.Json.Serialization;

namespace Domain.Enums
{
    /// <summary>
    /// Academic degrees that a doctor may hold.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum DegreeType
    {
        Professor,
        Doctor,
        Master,
        Bachelor,
        Associate,
        Undergraduate,
        Graduate,
        PhD,
        PostDoc,
    }
}
