namespace Application.Features.Patients.DeletePatient;

public class DeletePatientCommand : ICommand
{
    /// <summary>
    /// Patient's ID
    /// </summary>
    public required Guid Id { get; set; }
}