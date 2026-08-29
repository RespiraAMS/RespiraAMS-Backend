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

    public required DiagnosisRecord DiagnosisRecord { get; set; }

    /// <summary>
    /// Treatment type
    /// </summary>
    public required TreatmentType TreatmentType { get; set; }
}

public class CreateTreatmentResult(Guid id)
{
    /// <summary>
    /// Treatment ID
    /// </summary>
    public Guid Id { get; set; } = id;
}
