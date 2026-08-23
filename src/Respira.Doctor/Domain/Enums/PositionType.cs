using System.Text.Json.Serialization;

namespace Domain.Enums
{
    /// <summary>
    /// Hospital positions in hierarchical order (lowest to highest).
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum PositionType
    {
        /// <summary>Staff doctor</summary>
        StaffDoctor = 1,

        /// <summary>Senior doctor</summary>
        SeniorDoctor = 2,

        /// <summary>Department deputy head</summary>
        DepartmentDeputyHead = 3,

        /// <summary>Department head</summary>
        DepartmentHead = 4,

        /// <summary>Deputy director</summary>
        DeputyDirector = 5,

        /// <summary>Director</summary>
        Director = 6,
    }
}
