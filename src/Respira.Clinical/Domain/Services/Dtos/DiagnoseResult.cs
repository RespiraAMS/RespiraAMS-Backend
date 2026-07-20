using Domain.Enums;
using Domain.Models;

namespace Domain.Services.Dtos;

public class DiagnoseResult
{
    /// <summary>
    /// Diagnose result: severity
    /// </summary>
    public required Severity Severity { get; set; }

    /// <summary>
    /// Diagnose result: patient treatment site
    /// </summary>
    public required TreatmentSite TreatmentSite { get; set; }

    /// <summary>
    /// Diagnose result: infection probabilities
    /// </summary>
    public List<InfectionProbability> InfectionProbabilities { get; set; } = [];

    /// <summary>
    /// Recommendation treatment protocols
    /// </summary>
    public List<EmpiricTreatmentProtocol> Recommendations { get; set; } = [];
}