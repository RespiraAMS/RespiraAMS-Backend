using Application.Features.ResistanceRiskFactors.CreateResistanceRiskFactor;
using Application.Features.ResistanceRiskFactors.UpdateResistanceRiskFactor;
using Application.Features.Shared.ManageCriterion;

namespace Respira.Clinical.API.Dtos;

public class CreateResistanceRiskFactorRequestDto
{
    public required Guid PathogenId { get; set; }
    public required CreateCriterionCommand Criterion { get; set; }
    public required string Name { get; set; }

    public CreateResistanceRiskFactorCommand ToCommand(Guid diseaseId)
    {
        return new CreateResistanceRiskFactorCommand
        {
            DiseaseId = diseaseId,
            PathogenId = PathogenId,
            Criterion = Criterion,
            Name = Name
        };
    }
}

public class UpdateResistanceRiskFactorRequestDto
{
    public required Guid PathogenId { get; set; }
    public required UpdateCriterionCommand Criterion { get; set; }
    public required string Name { get; set; }

    public UpdateResistanceRiskFactorCommand ToCommand(Guid id)
    {
        return new UpdateResistanceRiskFactorCommand
        {
            Id = id,
            PathogenId = PathogenId,
            Criterion = Criterion,
            Name = Name
        };
    }
}