using Application.Features.Shared.ManageCriterion;

namespace Application.Features.IcuHospitalizeCriteria.CreateIcuHospitalizeCriterion;

public class CreateIcuHospitalizeCriterionCommand : ICommand
{
    public required Guid DiseaseId { get; set; }
    public required CreateCriterionCommand Criterion { get; set; }
    public required int Score { get; set; }
}

public class CreateIcuHospitalizeCriterionResult(Guid id)
{
    public Guid Id { get; set; } = id;
}