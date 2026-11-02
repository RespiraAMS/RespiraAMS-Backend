using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Respira.Clinical.Domain.Enums
{
    /// <summary>
    /// Antibiotic route of administration
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum RouteOfAdministration
    {
        [Display(Name = "Oral")]
        [Description("Consume through mouth")]
        Oral,

        [Display(Name = "Intravenous")]
        [Description("Consume through vein")]
        Intravenous,
    }
}
