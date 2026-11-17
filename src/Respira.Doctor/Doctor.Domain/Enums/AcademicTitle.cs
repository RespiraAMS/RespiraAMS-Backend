using System.Text.Json.Serialization;

namespace Respira.Doctor.Domain.Enums
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum AcademicTitle
    {
        /// <summary>No academic title</summary>
        None = 0,

        /// <summary>Associate professor</summary>
        AssociateProfessor = 1,

        /// <summary>Professor</summary>
        Professor = 2,
    }
}
