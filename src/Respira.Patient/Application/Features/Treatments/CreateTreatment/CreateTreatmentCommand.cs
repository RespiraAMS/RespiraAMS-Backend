using Domain.Enums;

namespace Application.Features.Treatments.CreateTreatment;

public class CreateTreatmentCommand : ICommand
{
    /// <summary>
    /// Patient ID
    /// </summary>
    public required Guid PatientId { get; set; }

    /// <summary>
    /// Doctor ID: ID of the doctor who responsible for this treatment
    /// </summary>
    public required Guid DoctorId { get; set; }

    /// <summary>
    /// Patient's creatine clearance level (in mg/dL) measured
    /// at the time of treatment. This can be used to audit for
    /// antibiotic dosage
    /// </summary>
    public required decimal Crcl { get; set; }

    /// <summary>
    /// List of medicines used for this treatment. This list must not empty
    /// </summary>
    public required List<MedicineRecord> MedicineRecords { get; set; }

    /// <summary>
    /// Treatment type
    /// </summary>
    public required TreatmentType TreatmentType { get; set; }

    /// <summary>
    /// Patient diagnosis result: severity
    /// </summary>
    public string? Severity { get; set; }

    /// <summary>
    /// Patient diagnosis result: treatment site
    /// </summary>
    public string? TreatmentSite { get; set; }

    /// <summary>
    /// Patient diagnosis result: suspected infection probabilities
    /// </summary>
    public List<InfectionProbabilityRecord>? InfectionProbabilityRecords { get; set; } = [];

    /// <summary>
    /// Name of the pathogen that cause patient to got disease
    /// </summary>
    public string? Pathogen { get; set; }
}

public class CreateTreatmentResult(Guid id)
{
    /// <summary>
    /// Treatment ID
    /// </summary>
    public Guid Id { get; set; } = id;
}
