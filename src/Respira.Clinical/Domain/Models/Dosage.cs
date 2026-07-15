using Domain.Enums;

namespace Domain.Models;

/*
 * For simplicity, dose simply is just a string, instead of splitting it into value, unit,... since some
 * dose can have additional note, quirky value,... and even doctor don't need such precise control 
 */

/// <summary>
/// Antibiotic dosage
/// </summary>
public class Dosage : Base
{
    /// <summary>
    /// Route of administration
    /// </summary>
    public required RouteOfAdministration RouteOfAdministration { get; set; }
    /// <summary>
    /// Antibiotic dose.
    /// </summary>
    public required string Dose { get; set; }
    /// <summary>
    /// Glomerular filtration rate (GFR): measures how effectively your kidneys filter waste and
    /// excess fluid from your blood
    /// </summary>
    public required decimal? GlomerularFiltrationRate { get; set; }
}