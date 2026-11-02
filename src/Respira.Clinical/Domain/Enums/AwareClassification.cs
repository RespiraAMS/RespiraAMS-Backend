using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Respira.Clinical.Domain.Enums
{
    /// <summary>
    /// WHO's AWaRe antibiotic classification
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum AwareClassification
    {
        [Display(Name = "Access")] Access,
        [Display(Name = "Access-Watch")] AccessWatch,
        [Display(Name = "Watch")] Watch,
        [Display(Name = "Reserve")] Reserve,
        [Display(Name = "Others")] Others,
        [Display(Name = "Unclassified")] Unclassified,
    }
}
