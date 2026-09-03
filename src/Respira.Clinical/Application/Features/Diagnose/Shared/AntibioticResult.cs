using Domain.Enums;

namespace Application.Features.Diagnose.Shared;

public record AntibioticResult
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
    /// Antibiotic classification (WHO's AWaRe classification)
    /// </summary>
    public required AwareClassification Classification { get; set; }

    /// <summary>
    /// Antibiotic dosages (already adjusted)
    /// </summary>
    public required List<DosageResult> Dosages { get; set; }
}
