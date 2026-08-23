using System.Text.Json.Serialization;

namespace Domain.Enums
{
    /// <summary>
    /// Gender of a doctor.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum GenderType
    {
        /// <summary>Male</summary>
        Male,

        /// <summary>Female</summary>
        Female,
    }
}
