namespace Application.Features.AntibioticGroups.CreateAntibioticGroup;

public class CreateAntibioticGroupCommand : ICommand
{
    /// <summary>
    /// Antibiotic group name
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Antibiotic group description
    /// </summary>
    public required string Description { get; set; }

    /// <summary>
    /// Antibiotic group parent group ID. For example, penicilin has beta-lactam as parent
    /// (or penicilin is a subgroup of beta-lactam)
    /// </summary>
    public required Guid? ParentId { get; set; }
}

public class CreateAntibioticGroupResult(Guid id)
{
    /// <summary>
    /// Antibiotic group ID
    /// </summary>
    public Guid Id { get; set; } = id;
}