using Application.Features.Diagnose.Shared;
using Domain.Enums;
using Severity = Domain.Enums.Severity;

namespace Application.Features.Diagnose.EmpiricalDiagnose;

public class EmpiricalDiagnoseQuery : IQuery
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
    /// Patient's height in meter
    /// </summary>
    public required decimal Height { get; set; }


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

public class InfectionProbability
{
    /// <summary>
    /// Pathogen ID
    /// </summary>
    public required Guid PathogenId { get; set; }

    /// <summary>
    /// Pathogen name
    /// </summary>
    public required string PathogenName { get; set; }

    /// <summary>
    /// Infection probability, from 0 to 1
    /// </summary>
    public required decimal Probability { get; set; }
}

public class EmpiricalTreatmentProtocolResult
{
    /// <summary>
    /// Treatment protocol ID
    /// </summary>
    public required Guid Id { get; set; }

    /// <summary>
    /// Treatment protocol updated timestamp
    /// </summary>
    public required DateTimeOffset UpdatedAt { get; set; }

    /// <summary>
    /// Treatment protocol name
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Treatment protocol issuer
    /// </summary>
    /// <example>
    /// WHO, VietNam Ministry of Health
    /// </example>
    public required string Issuer { get; set; }

    /// <summary>
    /// Treatment protocol issue date
    /// </summary>
    public required DateOnly IssueDate { get; set; }

    /// <summary>
    /// Treatment protocol version (1-based index)
    /// </summary>
    public required int Version { get; set; }

}
public class EmpiricalDiagnoseResult
{
    /// <summary>
    /// Patient's creatine clearance calculated
    /// </summary>
    public required decimal Crcl { get; set; }

    /// <summary>
    /// List of recommended medicines
    /// </summary>
    public required List<AntibioticResult> Recommendations { get; set; } = [];

    /// <summary>
    /// List of all medicines that are relevent with patient's symptoms.
    /// Even if doctors disagree with the Recommendations list,
    /// they should only picked medicines from this list
    /// </summary>
    public required List<AntibioticResult> Medicines { get; set; } = [];

    /// <summary>
    /// Patient's severity
    /// </summary>
    public required Severity Severity { get; set; }

    /// <summary>
    /// Patient's treatment site
    /// </summary>
    public required TreatmentSite TreatmentSite { get; set; }

    /// <summary>
    /// Patient's infection probability
    /// </summary>
    public required List<InfectionProbability> InfectionProbabilities { get; set; }

    /// <summary>
    /// List of treatment protocols used for reference when diagnosing
    /// </summary>
    public required List<EmpiricalTreatmentProtocolResult> References { get; set; } = [];
}
