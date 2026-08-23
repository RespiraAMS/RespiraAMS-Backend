using System.Text.Json.Serialization;

namespace Domain.Enums
{
    /// <summary>
    /// User roles in the system. Determines access level and permissions.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum RoleType
    {
        Doctor,
        Manager,
        Admin,
    }
}
