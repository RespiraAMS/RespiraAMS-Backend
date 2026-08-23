using System.Text.Json.Serialization;

namespace Domain.Enums
{
    /// <summary>
    /// Academic titles held by doctors.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum AcademicTitleEnum
    {
        None = 0,
        AssociateProfessor = 1,
        Professor = 2,
    }
}
