namespace Application.Features.AntibioticGroups.CreateAntibioticGroup;

public class CreateAntibioticGroupCommand : ICommand
{
    public required string Name { get; set; }
    public required string Description { get; set; }
    public required Guid? ParentId { get; set; }
}

public class CreateAntibioticGroupResult(Guid id)
{
    public Guid Id { get; set; } = id;
}