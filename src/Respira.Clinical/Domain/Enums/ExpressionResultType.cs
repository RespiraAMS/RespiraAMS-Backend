using System.Text.Json.Serialization;

namespace Respira.Domain.Enums
{
    /// <summary>
    /// Expression result type
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum ExpressionResultType
    {
        Numeric,
        Boolean,
    }
}
