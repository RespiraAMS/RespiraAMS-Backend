using Application.Features.Shared.ManageCriterion;

namespace Application.Features.EmpiricTreatmentProtocols.AddNewCriteria;

public class AddNewCriteriaCommand : ICommand
{
    public Guid Id { get; set; }
    public List<CreateCriterionCommand> Criteria { get; set; } = [];
}