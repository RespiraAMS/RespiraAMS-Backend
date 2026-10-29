using System.Text.Json.Serialization;

namespace Respira.Clinical.Domain.Enums
{
    /// <summary>
    /// NOTE: some of these infections are already covered in some
    /// clinical variables, but we still add them here.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum InfectionType
    {
    }
}
