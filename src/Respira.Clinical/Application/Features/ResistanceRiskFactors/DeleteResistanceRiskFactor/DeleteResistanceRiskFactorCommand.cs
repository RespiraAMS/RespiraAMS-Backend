namespace Application.Features.ResistanceRiskFactors.DeleteResistanceRiskFactor;

public record DeleteResistanceRiskFactorCommand(Guid Id) : ICommand
{
    /// <summary>
    /// Resistance risk factor ID
    /// </summary>
    public Guid Id { get; set; } = Id;
}