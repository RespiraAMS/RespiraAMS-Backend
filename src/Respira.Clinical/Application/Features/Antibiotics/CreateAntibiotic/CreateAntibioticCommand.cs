using Domain.Enums;

namespace Application.Features.Antibiotics.CreateAntibiotic;

public class CreateAntibioticCommand : ICommand
{
    public required string Name { get; set; }
    public required Guid AntibioticGroupId { get; set; }
    public required AwareCategory Category { get; set; }
}

public class CreateAntibioticResult(Guid id)
{
    public Guid Id { get; set; } = id;
}