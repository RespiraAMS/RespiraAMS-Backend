using Application.Features.IcuHospitalizeCriteria.CreateIcuHospitalizeCriterion;
using Application.Features.IcuHospitalizeCriteria.UpdateIcuHospitalizeCriterion;
using Application.Features.Shared.ManageCriterion;

namespace Respira.Clinical.API.Dtos;

public class CreateIcuHospitalizeCriterionRequestDto
{
    public required CreateCriterionCommand Criterion { get; set; }
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
    public required UpdateCriterionCommand Criterion { get; set; }
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