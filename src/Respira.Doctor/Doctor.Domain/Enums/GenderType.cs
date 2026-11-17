using System.Text.Json.Serialization;

namespace Respira.Doctor.Domain.Enums
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum GenderType
    {
        /// <summary>Male</summary>
        Male,

        /// <summary>Female</summary>
        Female,
    }
}
