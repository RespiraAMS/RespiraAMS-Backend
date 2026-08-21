using Domain.Enums;

namespace Application.Features.Antibiotics.CreateAntibiotic;

public class CreateAntibioticCommand : ICommand
{
    /// <summary>
    /// Antibiotic name
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Antibiotic group ID
    /// </summary>
    public required Guid AntibioticGroupId { get; set; }

    /// <summary>
    /// Antibiotic WHO's AWaRe category
    /// </summary>
    public required AwareClassification Classification { get; set; }

    /// <summary>
    /// Route of administration of antibiotic's standard dose
    /// </summary>
    public required RouteOfAdministration RouteOfAdministration { get; set; }

    /// <summary>
    /// Standard dose
    /// </summary>
    public required string StandardDose { get; set; }
}

public class CreateAntibioticResult(Guid id)
{
    /// <summary>
    /// Antibiotic ID
    /// </summary>
    public Guid Id { get; set; } = id;
}
