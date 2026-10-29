using Respira.Clinical.Domain.Enums;
using Respira.Clinical.Domain.Models;
using Respira.ServiceDefaults.Contracts.CQRS;

namespace Respira.Clinical.Application.Features.Diagnosis.Diagnose
{
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
        public IEnumerable<Guid> AllergicAntibiotics { get; set; } = [];

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
        public required Guid Pathogen { get; set; }
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

    public record ClinicalVariableResult(Guid Id, string Name, string Code);

    public record PathogenResult(Guid Id, string Name);

    public record AntibioticResult(Guid Id, string Name);

    public record CriterionResult(Guid Id, string Name, string Formula);

    public record AntibiogramResult
    {
        public required Guid Id { get; set; }
        public required MicCategory Category { get; set; }
        public required PathogenResult Pathogen { get; set; }
        public required IEnumerable<AntibioticResult> Antibiotics { get; set; }
    }

    public record TreatmentProtocolResult
    {
        public required Guid Id { get; set; }
        public required Severity Severity { get; set; }
        public required TreatmentSite TreatmentSite { get; set; }
        public IEnumerable<IEnumerable<AntibioticResult>> Medicines { get; set; } = [];
        public IEnumerable<PathogenResult> Pathogens { get; set; } = [];
        public IEnumerable<CriterionResult> Criteria { get; set; } = [];
    }

    public record DiagnoseResult
    {
        public required Severity Severity { get; set; }
        public required TreatmentSite TreatmentSite { get; set; }
        public required IEnumerable<MetricsSeverityDiagnosis> MetricsSeverityDiagnosis { get; set; }
        public required IEnumerable<PathogenResult> WorthSuspected { get; set; }
        public required IEnumerable<PathogenResult> HeavySuspected { get; set; }
        public required IEnumerable<Evidence> Evidences { get; set; }
        public required IEnumerable<ClinicalVariableResult> MissingVariables { get; set; }
        public required bool IsPatientPregnantOrInLactationPhase { get; set; }
        public double? Egfr { get; set; }
        public IEnumerable<InfectionType> OtherInfections { get; set; } = [];
        public required IEnumerable<AntibiogramResult> MicrobialTestResult { get; set; }
        public required IEnumerable<TreatmentProtocolResult> Protocols { get; set; }
    }
}
