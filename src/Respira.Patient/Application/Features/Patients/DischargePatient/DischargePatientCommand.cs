using Domain.Enums;

namespace Application.Features.Patients.DischargePatient;

public class DischargePatientCommand : ICommand
{
    /// <summary>
    /// Patient's ID
    /// </summary>
    public required Guid Id { get; set; }

    /// <summary>
    /// Patient's status
    /// </summary>
    public required PatientStatus Status { get; set; }
}