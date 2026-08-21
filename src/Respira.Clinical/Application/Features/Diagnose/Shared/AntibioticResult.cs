namespace Application.Features.Diagnose.Shared;

public class AntibioticResult
{
    /// <summary>
    /// Antibiotic ID
    /// </summary>
    public required Guid Id { get; set; }

    /// <summary>
    /// Antibiotic name
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Antibiotic group ID
    /// </summary>
    public required Guid AntibioticGroupId { get; set; }

    /// <summary>
    /// Antibiotic group name
    /// </summary>
    public required string AntibioticGroupName { get; set; }

    /// <summary>
    /// Antibiotic adjusted dose, based on patient's CrCl calculated when diagnosing
    /// </summary>
    public required string Dose { get; set; }
}
