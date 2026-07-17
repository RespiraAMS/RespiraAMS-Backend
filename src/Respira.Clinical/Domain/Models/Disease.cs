namespace Domain.Models;

/// <summary>
/// Disease
/// </summary>
public class Disease
{
    /// <summary>
    /// Disease name
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Disease description
    /// </summary>
    public required string Description { get; set; }

    /// <summary>
    /// ICU score threshold. By default, score threshold is inclusive (for example, if score = 3 then the logic
    /// check for ICU hospitalizing is &gt;= 3) 
    /// </summary>
    public required int IcuScoreThreshold { get; set; }

    /// <summary>
    /// List of disease causes
    /// </summary>
    public List<Cause> Causes { get; set; } = [];

    /// <summary>
    /// List of disease's ICU hospitalize criteria
    /// </summary>
    public List<IcuHospitalizeCriterion> IcuHospitalizeCriteria { get; set; } = [];

    /// <summary>
    /// List of disease's resistance risk factors
    /// </summary>
    public List<ResistanceRiskFactor> ResistanceRiskFactors { get; set; } = [];

    /// <summary>
    /// List of disease's treatment protocols
    /// </summary>
    public List<TreatmentProtocol> TreatmentProtocols { get; set; } = [];
}