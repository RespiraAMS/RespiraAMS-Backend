using Respira.Clinical.Domain.Enums;
using Respira.Clinical.Domain.Models;
using Respira.ServiceDefaults.Contracts.CQRS;

namespace Respira.Clinical.Application.Features.Diagnosis.Diagnose
{
    /// <summary>
    /// Observation query
    /// </summary>
    /// <param name="VariableId">Clinical variable ID</param>
    /// <param name="Value">
    /// Clinical observation value. Since it's a string, make sure that
    /// you pass correct value, or the parsing will fail
    /// </param>
    public record ObservationQuery(Guid VariableId, string Value);

    public record AllergyQuery
    {
        /// <summary>
        /// Type of allergy
        /// </summary>
        public required AllergyType Type { get; set; }

        /// <summary>
        /// The list of antibiotic IDs that patient's allergic to
        /// </summary>
        public IEnumerable<Guid> AllergicAntibioticIds { get; set; } = [];

        /// <summary>
        /// Other allergy, corresponding to <see cref="AllergyType.Unknown"/> or
        /// <see cref="AllergyType.NonRegisteredAntibiotic"/>
        /// </summary>
        public string? OtherAllergy { get; set; }
    }

    public record MicrobialSampleQuery
    {
        /// <summary>
        /// Microbial sample type
        /// </summary>
        public required MicrobialSampleType SampleType { get; set; }

        /// <summary>
        /// MIC category
        /// </summary>
        public required MicCategory MicCategory { get; set; }

        /// <summary>
        /// Pathogen ID
        /// </summary>
        public required Guid PathogenId { get; set; }
    }

    public record DiagnoseQuery : IQuery
    {
        public required IEnumerable<ObservationQuery> Observations { get; set; }

        /// <summary>
        /// Patient allergy status
        /// </summary>
        public required AllergyQuery Allergy { get; set; }

        /// <summary>
        /// Special medical history that the system does not cover.
        /// This would be used to warn doctor before making final decision,
        /// and audit when revisit treatment
        /// </summary>
        public IEnumerable<string> SpecialMedicalHistory { get; set; } = [];

        /// <summary>
        /// Other active infections, excluding pneumonia.
        /// </summary>
        public IEnumerable<InfectionType> OtherInfections { get; set; } = [];

        /// <summary>
        /// Microbial samples
        /// </summary>
        public IEnumerable<MicrobialSampleQuery> MicrobialSamples { get; set; } = [];
    }

    /// <summary>
    /// Clinical variable result
    /// </summary>
    /// <param name="Id">Clinical variable ID</param>
    /// <param name="Name">Clinical variable name</param>
    /// <param name="Code">Clinical variable code</param>
    public record ClinicalVariableResult(Guid Id, string Name, string Code);

    /// <summary>
    /// Pathogen result
    /// </summary>
    /// <param name="Id">Pathogen ID</param>
    /// <param name="Name">Pathogen name</param>
    public record PathogenResult(Guid Id, string Name);

    /// <summary>
    /// Antibiotic result
    /// </summary>
    /// <param name="Id">Antibiotic ID</param>
    /// <param name="Name">Antibiotic name</param>
    /// <param name="Classification">Antibiotic's AWaRe classification</param>
    public record AntibioticResult(Guid Id, string Name, AwareClassification Classification);

    /// <summary>
    /// Criterion result
    /// </summary>
    /// <param name="Id">Criterion ID</param>
    /// <param name="Name">Criterion name</param>
    /// <param name="Formula">Criterion formula</param>
    public record CriterionResult(Guid Id, string Name, string Formula);

    public record AllergyResult
    {
        /// <summary>
        /// Type of allergy
        /// </summary>
        public required AllergyType Type { get; set; }

        /// <summary>
        /// The list of antibiotic IDs that patient's allergic to
        /// </summary>
        public IEnumerable<AntibioticResult> AllergicAntibiotics { get; set; } = [];

        /// <summary>
        /// Other allergy, corresponding to <see cref="AllergyType.Unknown"/> or
        /// <see cref="AllergyType.NonRegisteredAntibiotic"/>
        /// </summary>
        public string? OtherAllergy { get; set; }
    }

    public record MicrobialTestResult
    {
        /// <summary>
        /// Microbial sample type
        /// </summary>
        public required MicrobialSampleType SampleType { get; set; }

        /// <summary>
        /// Antibiogram's MIC category
        /// </summary>
        public required MicCategory Category { get; set; }

        /// <summary>
        /// Antibiogram's pathogen
        /// </summary>
        public required PathogenResult Pathogen { get; set; }

        /// <summary>
        /// Antibiogram's medicine list
        /// </summary>
        public required IEnumerable<AntibioticResult> Antibiotics { get; set; }
    }

    public record TreatmentProtocolResult
    {
        /// <summary>
        /// Treatment protocol ID
        /// </summary>
        public required Guid Id { get; set; }

        /// <summary>
        /// Treatment protocol's severity
        /// </summary>
        public required Severity Severity { get; set; }

        /// <summary>
        /// Treatment protocol's treatment site
        /// </summary>
        public required TreatmentSite TreatmentSite { get; set; }

        /// <summary>
        /// Treatment protocol's treatment medicine (list of medicine combinations)
        /// </summary>
        public IEnumerable<IEnumerable<AntibioticResult>> Medicines { get; set; } = [];

        /// <summary>
        /// List of pathogens that this treatment protocol is used for
        /// </summary>
        public IEnumerable<PathogenResult> Pathogens { get; set; } = [];

        /// <summary>
        /// List of criteria that this treatment protocol is used for
        /// </summary>
        public IEnumerable<CriterionResult> Criteria { get; set; } = [];
    }

    public record DiagnoseResult
    {
        public required Guid DiagnosisId { get; set; }

        /// <summary>
        /// Final severity diagnosis
        /// </summary>
        public required Severity Severity { get; set; }

        /// <summary>
        /// Final treatment site diagnosis
        /// </summary>
        public required TreatmentSite TreatmentSite { get; set; }

        /// <summary>
        /// List of individual clinical metrics severity diagnosis. Note that
        /// this also include evidences, which will duplicate in <see cref="Evidences" />
        /// </summary>
        public required IEnumerable<MetricsSeverityDiagnosis> MetricsSeverityDiagnosis { get; set; }

        /// <summary>
        /// List of worth suspected pathogens. Note that this list is not ranking,
        /// every pathogens in the entry is equal to each other
        /// </summary>
        public required IEnumerable<PathogenResult> WorthSuspected { get; set; }

        /// <summary>
        /// List of heavy suspected pathogens. Note that this list is not ranking,
        /// every pathogens in the entry is equal to each other
        /// </summary>
        public required IEnumerable<PathogenResult> HeavySuspected { get; set; }

        /// <summary>
        /// List of total evidences that are produced when performing severity diagnosis
        /// and infection assessment
        /// </summary>
        public required IEnumerable<Evidence> Evidences { get; set; }

        /// <summary>
        /// List of missing variables (variables that does not have observation)
        /// </summary>
        public required IEnumerable<ClinicalVariableResult> MissingVariables { get; set; }

        /// <summary>
        /// Boolean flag: is patient pregnant or in lactating phase
        /// </summary>
        public required bool IsPatientPregnantOrInLactationPhase { get; set; }

        /// <summary>
        /// Patient's eGFR. This value would be null if not enough information
        /// is provided when performing diagnosis
        /// </summary>
        public double? Egfr { get; set; }

        /// <summary>
        /// List of other infections outside of pneumonia infection
        /// </summary>
        public IEnumerable<InfectionType> OtherInfections { get; set; } = [];

        /// <summary>
        /// Microbial test result
        /// </summary>
        public required IEnumerable<MicrobialTestResult> MicrobialTestResults { get; set; }

        /// <summary>
        /// Patient allergy
        /// </summary>
        public required AllergyResult Allergy { get; set; }

        /// <summary>
        /// List of recommended treatment protocols, based on the result of diagnosis
        /// </summary>
        public required IEnumerable<TreatmentProtocolResult> Protocols { get; set; }
    }
}
