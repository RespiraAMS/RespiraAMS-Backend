using System.Text.Json.Serialization;

namespace Domain.Enums
{
    /// <summary>
    /// Hospital positions in hierarchical order (lowest to highest).
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum PositionType
    {
        StaffDoctor = 1,
        SeniorDoctor = 2,
        DepartmentDeputyHead = 3,
        DepartmentHead = 4,
        DeputyDirector = 5,
        Director = 6,
    }
}
