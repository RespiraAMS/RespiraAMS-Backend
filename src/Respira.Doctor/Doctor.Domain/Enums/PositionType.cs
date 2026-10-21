using System.Text.Json.Serialization;

namespace Respira.Doctor.Domain.Enums
{
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
        HeadOfDepartment = 4,

        /// <summary>Deputy director</summary>
        DeputyDirector = 5,

        /// <summary>Director</summary>
        Director = 6,
    }
}
