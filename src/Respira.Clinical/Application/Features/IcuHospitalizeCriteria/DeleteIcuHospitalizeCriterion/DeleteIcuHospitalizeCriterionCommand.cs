namespace Application.Features.IcuHospitalizeCriteria.DeleteIcuHospitalizeCriterion;

public class DeleteIcuHospitalizeCriterionCommand : ICommand
{
    /// <summary>
    /// ICU hospitalize criterion ID
    /// </summary>
    public required Guid Id { get; set; }
}