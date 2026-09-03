using Application.Features.Shared.ManageCriterion;

namespace Application.Features.IcuHospitalizeCriteria.CreateIcuHospitalizeCriterion;

public record CreateIcuHospitalizeCriterionCommand : ICommand
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

public record CreateIcuHospitalizeCriterionResult(Guid Id)
{
    /// <summary>
    /// ICU hospitalize criterion
    /// </summary>
    public Guid Id { get; set; } = Id;
}