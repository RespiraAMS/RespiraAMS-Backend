using System.Text.Json.Serialization;

namespace Domain.Enums
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum AcademicTitleEnum
    {
        None = 0,
        AssociateProfessor = 1,
        Professor = 2,
    }
}
