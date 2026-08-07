namespace Application.Features.Diagnose;

public class DiagnoseQuery : IQuery
{
    /// <summary>
    /// Disease ID
    /// </summary>
    public required Guid DiseaseId { get; set; }

    /// <summary>
    /// Patient's date of birth
    /// </summary>
    /// <example>1980-07-21</example>
    public required DateOnly DateOfBirth { get; set; }

    /// <summary>
    /// Patient gender
    /// </summary>
    public required bool IsMale { get; set; }

    /// <summary>
    /// Patient's weight, in kg
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
    /// Patient's systolic blood pressure (mmHg)
    /// </summary>
    public required decimal SystolicBloodPressure { get; set; }

    /// <summary>
    /// Patient's diastolic blood pressure
    /// </summary>
    public required decimal DiastolicBloodPressure { get; set; }

    /// <summary>
    /// List of ICU hospitalize criteria IDs
    /// (It is the <see cref="Criterion"/> ID, not <see cref="IcuHospitalizeCriterion"/> ID)
    /// </summary>
    public required List<Guid> IcuHospitalizeCriteria { get; set; }

    /// <summary>
    /// List of resistance risk factor criteria IDs
    /// (It is the <see cref="Criterion"/> ID, not <see cref="ResistanceRiskFactor"/> ID)
    /// </summary>
    public required List<Guid> ResistanceRiskFactors { get; set; }

    /// <summary>
    /// List of other criteria IDs
    /// </summary>
    public required List<Guid> OtherCriteria { get; set; }
}