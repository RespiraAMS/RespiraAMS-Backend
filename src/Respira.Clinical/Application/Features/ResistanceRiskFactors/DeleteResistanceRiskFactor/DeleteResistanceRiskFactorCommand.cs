namespace Application.Features.ResistanceRiskFactors.DeleteResistanceRiskFactor;

public class DeleteResistanceRiskFactorCommand : ICommand
{
    /// <summary>
    /// Resistance risk factor ID
    /// </summary>
    public required Guid Id { get; set; }
}