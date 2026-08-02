using Application.Features.Shared.ManageCriterion;

namespace Application.Features.IcuHospitalizeCriteria.UpdateIcuHospitalizeCriterion;

public class UpdateIcuHospitalizeCriterionCommand : ICommand
{
    public required Guid Id { get; set; }
    public required UpdateCriterionCommand Criterion { get; set; }
    public required int Score { get; set; }
}