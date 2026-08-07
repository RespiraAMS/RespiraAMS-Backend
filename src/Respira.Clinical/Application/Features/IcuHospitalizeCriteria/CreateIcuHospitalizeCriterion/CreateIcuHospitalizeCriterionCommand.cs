using Application.Features.Shared.ManageCriterion;

namespace Application.Features.IcuHospitalizeCriteria.CreateIcuHospitalizeCriterion;

public class CreateIcuHospitalizeCriterionCommand : ICommand
{
    /// <summary>
    /// Disease ID
    /// </summary>
    public required Guid DiseaseId { get; set; }

    /// <summary>
    /// Criterion
    /// </summary>
    public required CreateCriterionCommand Criterion { get; set; }

    /// <summary>
    /// ICU score
    /// </summary>
    public required int Score { get; set; }
}

public class CreateIcuHospitalizeCriterionResult(Guid id)
{
    /// <summary>
    /// ICU hospitalize criterion
    /// </summary>
    public Guid Id { get; set; } = id;
}