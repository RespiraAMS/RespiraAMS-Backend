using Domain.Enums;

namespace Application.Features.Diagnose.Shared;

public class DosageResult
{
    /// <summary>
    /// Antibiotic route of administration
    /// </summary>
    public required RouteOfAdministration RouteOfAdministration { get; set; }

    /// <summary>
    /// Antibiotic adjusted dose, based on patient's CrCl calculated when diagnosing
    /// </summary>
    public required string Dose { get; set; }

}
