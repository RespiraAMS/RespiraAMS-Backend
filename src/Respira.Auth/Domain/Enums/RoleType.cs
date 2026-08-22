using System.Text.Json.Serialization;

namespace Domain.Enums
{
    /// <summary>
    /// Role type for the AuthDoctor and AuthAdmin
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum RoleType
    {
        /// <summary>
        /// Doctor role
        /// </summary>
        Doctor,

        /// <summary>
        /// Manager role
        /// </summary>
        Manager,

        /// <summary>
        /// Admin role
        /// </summary>
        Admin,
    }
}
