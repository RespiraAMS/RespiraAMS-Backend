namespace Domain.Services.Dtos;

/// <summary>
/// Patient clinical picture used for empirical diagnosis
/// </summary>
public class ClinicalPicture
{
    /// <summary>
    /// Boolean flag: Does patient have a state of decrease consciousness
    /// </summary>
    public required bool Confusion { get; set; }

    /// <summary>
    /// Patient's urea in blood (mmol/L)
    /// </summary>
    public required decimal? Urea { get; set; }

    /// <summary>
    /// Patient's respiratory per minute
    /// </summary>
    public required int Respiratory { get; set; }

    /// <summary>
    /// Patient's systolic blood pressure (mmHg)
    /// </summary>
    public required decimal SystolicBloodPressure { get; set; }

    /// <summary>
    /// Patient's diastolic blood pressure
    /// </summary>
    public required decimal DiastolicBloodPressure { get; set; }

    /// <summary>
    /// List of ICU hospitalize criteria IDs
    /// </summary>
    public required List<Guid> IcuHospitalizeCriteria { get; set; }

    /// <summary>
    /// List of resistance risk factor criteria IDs
    /// </summary>
    public required List<Guid> ResistanceRiskFactors { get; set; }

    /// <summary>
    /// List of other criteria IDs
    /// </summary>
    public required List<Guid> OtherCriteria { get; set; }
}
