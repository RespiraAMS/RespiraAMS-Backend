using System.ComponentModel;
using Application.Features.Diagnose.Shared;
using Domain.Enums;

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
    public required Guid PathogenId { get; set; }
    public required string PathogenName { get; set; }
    public required decimal Probability { get; set; }
}

public class EmpiricalTreatmentProtocolResult
{
    /// <summary>
    /// Treatment protocol ID
    /// </summary>
    public required Guid Id { get; set; }

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
    public required decimal Crcl { get; set; }
    public required List<AntibioticResult> Recommendations { get; set; } = [];
    public required List<AntibioticResult> Medicines { get; set; } = [];
    public required Domain.Enums.Severity Severity { get; set; }
    public required TreatmentSite TreatmentSite { get; set; }
    public required List<InfectionProbability> InfectionProbabilities { get; set; }
    public required List<EmpiricalTreatmentProtocolResult> References { get; set; } = [];
}
