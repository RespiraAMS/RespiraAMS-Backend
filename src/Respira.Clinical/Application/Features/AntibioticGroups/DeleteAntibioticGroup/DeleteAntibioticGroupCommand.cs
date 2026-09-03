namespace Application.Features.AntibioticGroups.DeleteAntibioticGroup;

public record DeleteAntibioticGroupCommand(Guid Id) : ICommand
{
    /// <summary>
    /// Antibiotic group ID
    /// </summary>
    public Guid Id { get; set; } = Id;
}