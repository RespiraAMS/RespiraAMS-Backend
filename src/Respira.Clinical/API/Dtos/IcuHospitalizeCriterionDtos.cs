using Application.Features.IcuHospitalizeCriteria.CreateIcuHospitalizeCriterion;
using Application.Features.IcuHospitalizeCriteria.UpdateIcuHospitalizeCriterion;
using Application.Features.Shared.ManageCriterion;

namespace Respira.Clinical.API.Dtos;

public class CreateIcuHospitalizeCriterionRequestDto
{
    /// <summary>
    /// Criterion
    /// </summary>
    public required CreateCriterionCommand Criterion { get; set; }

    /// <summary>
    /// ICU score
    /// </summary>
    public required int Score { get; set; }

    public CreateIcuHospitalizeCriterionCommand ToCommand(Guid diseaseId)
    {
        return new CreateIcuHospitalizeCriterionCommand
        {
            DiseaseId = diseaseId,
            Criterion = Criterion,
            Score = Score
        };
    }
}

public class UpdateIcuHospitalizeCriterionRequestDto
{
    /// <summary>
    /// Criterion
    /// </summary>
    public required UpdateCriterionCommand Criterion { get; set; }

    /// <summary>
    /// ICU score
    /// </summary>
    public required int Score { get; set; }

    public UpdateIcuHospitalizeCriterionCommand ToCommand(Guid id)
    {
        return new UpdateIcuHospitalizeCriterionCommand
        {
            Id = id,
            Criterion = Criterion,
            Score = Score
        };
    }
}