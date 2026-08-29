using System.Text.Json.Serialization;

namespace Domain.Enums
{
    /// <summary>
    /// User roles in the system. Determines access level and permissions.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum RoleType
    {
        /// <summary>A standard doctor account.</summary>
        Doctor,

        /// <summary>A manager account with elevated permissions.</summary>
        Manager,

        /// <summary>An administrator account with full access.</summary>
        Admin,
    }
}
