namespace Application.Features.AntibioticGroups.DeleteAntibioticGroup;

public class DeleteAntibioticGroupCommand : ICommand
{
    public required Guid Id { get; set; }
}