namespace Application.Features.AntibioticGroups.UpdateAntibioticGroup;

public class UpdateAntibioticGroupCommand : ICommand
{
    public required Guid Id { get; set; }
    public required string Name { get; set; }
    public required string Description { get; set; }
    public required Guid? ParentId { get; set; }
}