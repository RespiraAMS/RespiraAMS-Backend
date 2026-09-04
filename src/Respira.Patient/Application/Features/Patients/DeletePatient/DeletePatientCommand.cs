namespace Application.Features.Patients.DeletePatient;

public record DeletePatientCommand(Guid Id) : ICommand
{
    /// <summary>
    /// Patient's ID
    /// </summary>
    public Guid Id { get; set; } = Id;
}
