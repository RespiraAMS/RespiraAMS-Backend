using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Respira.Domain.Enums
{
    /// <summary>
    /// Disease severity
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum Severity
    {
        [Display(Name = "Mild")] Mild = 1,
        [Display(Name = "Moderate")] Moderate = 2,
        [Display(Name = "Severe")] Severe = 3
    }
}
