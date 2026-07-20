namespace Domain.Services.Dtos;

/// <summary>
/// Patient clinical picture
/// </summary>
public class ClinicalPicture
{
    /// <summary>
    /// Patient's name
    /// </summary>
    public required string Patient { get; set; }

    /// <summary>
    /// Patient's date of birth
    /// </summary>
    public required DateOnly DateOfBirth { get; set; }

    /// <summary>
    /// Patient gender
    /// </summary>
    public required bool IsMale { get; set; }

    /// <summary>
    /// Patient's weight
    /// </summary>
    public required decimal Weight { get; set; }

    /// <summary>
    /// Serum creatine used for calculate GFR
    /// </summary>
    public required decimal SerumCreatine { get; set; }

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
    /// Patient's systoloc blood pressure (mmHg)
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