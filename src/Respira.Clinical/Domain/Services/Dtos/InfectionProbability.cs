using Domain.Models;

namespace Domain.Services.Dtos;

/// <summary>
/// Infection probability.
/// </summary>
/// <param name="Pathogen">Pathogen</param>
/// <param name="Probability">Probability of having infected by this pathogen. Its value range from 0 to 1</param>
public record InfectionProbability(Pathogen Pathogen, decimal Probability);