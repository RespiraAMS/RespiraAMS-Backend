using Application.Features.Shared.ManageCriterion;

namespace Application.Features.EmpiricTreatmentProtocols.AddNewCriteria;

public record AddNewCriteriaCommand : ICommand
{
    /// <summary>
    /// Empiric treatment protocol
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// List of new criteria to be added
    /// </summary>
    public List<CreateCriterionCommand> Criteria { get; set; } = [];
}