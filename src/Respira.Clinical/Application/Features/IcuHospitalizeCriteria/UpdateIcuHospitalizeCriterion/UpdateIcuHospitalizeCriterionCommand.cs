using Application.Features.Shared.ManageCriterion;

namespace Application.Features.IcuHospitalizeCriteria.UpdateIcuHospitalizeCriterion;

public record UpdateIcuHospitalizeCriterionCommand : ICommand
{
    /// <summary>
    /// ICU hospitalize criterion ID
    /// </summary>
    public required Guid Id { get; set; }

    /// <summary>
    /// Criterion
    /// </summary>
    public required UpdateCriterionCommand Criterion { get; set; }

    /// <summary>
    /// ICU score
    /// </summary>
    public required int Score { get; set; }
}