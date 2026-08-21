using System.Text.Json.Serialization;

namespace Domain.Enums
{
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
