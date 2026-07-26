using Domain.Enums;

namespace Application.Features.Antibiotics.UpdateAntibiotic;

public class UpdateAntibioticCommand : ICommand
{
    public required Guid Id { get; set; }
    public required string Name { get; set; }
    public required Guid AntibioticGroupId { get; set; }
    public required AwareCategory Category { get; set; }
}