namespace Application.Features.AntibioticGroups.DeleteAntibioticGroup;

public class DeleteAntibioticGroupCommand : ICommand
{
    /// <summary>
    /// Antibiotic group ID
    /// </summary>
    public required Guid Id { get; set; }
}