using Domain.Enums;

namespace Application.Features.Treatments.CreateTreatment;

public record CreateTreatmentCommand : ICommand
{
    /// <summary>
    /// Patient ID
    /// </summary>
    public required Guid PatientId { get; set; }

    /// <summary>
    /// Doctor ID: ID of the doctor who responsible for this treatment
    /// </summary>
    public required Guid DoctorId { get; set; }

    public required DiagnosisRecord DiagnosisRecord { get; set; }

    /// <summary>
    /// Treatment type
    /// </summary>
    public required TreatmentType TreatmentType { get; set; }
}

public record CreateTreatmentResult(Guid Id)
{
    /// <summary>
    /// Treatment ID
    /// </summary>
    public Guid Id { get; set; } = Id;
}
