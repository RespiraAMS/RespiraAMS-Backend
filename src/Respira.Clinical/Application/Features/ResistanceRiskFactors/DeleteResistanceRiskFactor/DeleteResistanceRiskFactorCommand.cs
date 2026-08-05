namespace Application.Features.ResistanceRiskFactors.DeleteResistanceRiskFactor;

public class DeleteResistanceRiskFactorCommand : ICommand
{
    public required Guid Id { get; set; }
}