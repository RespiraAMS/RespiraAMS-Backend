namespace Domain.Models;

public record InfectionProbabilityRecord
{
    public required PathogenRecord Pathogen { get; init; }
    public required double Probability { get; init; }
}

