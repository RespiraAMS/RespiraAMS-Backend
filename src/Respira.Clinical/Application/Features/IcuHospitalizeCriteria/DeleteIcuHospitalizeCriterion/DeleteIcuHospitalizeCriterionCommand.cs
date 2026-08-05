namespace Application.Features.IcuHospitalizeCriteria.DeleteIcuHospitalizeCriterion;

public class DeleteIcuHospitalizeCriterionCommand : ICommand
{
    public required Guid Id { get; set; }
}