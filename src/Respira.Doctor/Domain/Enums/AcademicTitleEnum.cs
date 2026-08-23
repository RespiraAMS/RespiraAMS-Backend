using System.Text.Json.Serialization;

namespace Domain.Enums
{
    /// <summary>
    /// Academic titles held by doctors.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum AcademicTitleEnum
    {
        /// <summary>No academic title</summary>
        None = 0,

        /// <summary>Associate professor</summary>
        AssociateProfessor = 1,

        /// <summary>Professor</summary>
        Professor = 2,
    }
}
