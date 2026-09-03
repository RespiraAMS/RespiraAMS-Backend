namespace Application.Features.IcuHospitalizeCriteria.DeleteIcuHospitalizeCriterion;

public record DeleteIcuHospitalizeCriterionCommand(Guid Id) : ICommand
{
    /// <summary>
    /// ICU hospitalize criterion ID
    /// </summary>
    public Guid Id { get; set; } = Id;
}