namespace Application.Features.Antibiotics.GetAntibiotics;

public class GetAntibioticsQuery : IQuery;

public class AntibioticItem
{
    public required Guid Id { get; set; }
    public required string Name { get; set; }
}

public class GetAntibioticsResult(IEnumerable<AntibioticItem> antibiotics)
{
    public IEnumerable<AntibioticItem> Antibiotics { get; set; } = antibiotics;
}