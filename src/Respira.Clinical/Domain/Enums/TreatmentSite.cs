using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Respira.Clinical.Domain.Enums
{
    /// <summary>
    /// Where to receive treatment. It can also be used to deduce the severity of the patient,
    /// since they often go together (for example, an ICU mean Severe, and Inpatient often
    /// range from Moderate to Severe)
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum TreatmentSite
    {
        [Display(Name = "Outpatient")] Outpatient,
        [Display(Name = "Inpatient")] Inpatient,

        [Display(Name = "Intensive Care Unit (ICU)")]
        IntensiveCareUnit,
    }
}
