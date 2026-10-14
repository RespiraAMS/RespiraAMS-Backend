using System.Text.Json.Serialization;

namespace Respira.Doctor.Domain.Enums
{
    /// <summary>Kind of staff profile created during profile creation.</summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum ProfileRole
    {
        /// <summary>A standard doctor profile.</summary>
        Doctor,

        /// <summary>A manager profile with subordinates.</summary>
        Manager,
    }
}
