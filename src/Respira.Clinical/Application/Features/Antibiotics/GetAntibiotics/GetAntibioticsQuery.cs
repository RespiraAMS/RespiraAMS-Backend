namespace Application.Features.Antibiotics.GetAntibiotics;

public class GetAntibioticsQuery : IQuery;

public class AntibioticItem
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

public class GetAntibioticsResult(IEnumerable<AntibioticItem> antibiotics)
{
    /// <summary>
    /// List of antibiotics
    /// </summary>
    public IEnumerable<AntibioticItem> Antibiotics { get; set; } = antibiotics;
}
