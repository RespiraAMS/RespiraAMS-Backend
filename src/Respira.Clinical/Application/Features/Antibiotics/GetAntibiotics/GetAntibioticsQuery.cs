namespace Application.Features.Antibiotics.GetAntibiotics;

public record GetAntibioticsQuery : IQuery;

public record AntibioticItem
{
    /// <summary>
    /// Antibiotic ID
    /// </summary>
    public required Guid Id { get; set; }

    /// <summary>
    /// Antibiotic name
    /// </summary>
    public required string Name { get; set; }
}

public record GetAntibioticsResult(IEnumerable<AntibioticItem> Antibiotics)
{
    /// <summary>
    /// List of antibiotics
    /// </summary>
    public IEnumerable<AntibioticItem> Antibiotics { get; set; } = Antibiotics;
}
