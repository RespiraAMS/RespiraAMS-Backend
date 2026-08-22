namespace Application.Features.Patients.UpdatePatient;

public class UpdatePatientCommand : ICommand
{
    /// <summary>
    /// Patient's ID
    /// </summary>
    public required Guid Id { get; set; }

    /// <summary>
    /// Patient's fullname
    /// </summary>
    public required string FullName { get; set; }

    /// <summary>
    /// Patient's date of birth
    /// </summary>
    public required DateOnly DateOfBirth { get; set; }

    /// <summary>
    /// Patient's gender
    /// </summary>
    public required bool IsMale { get; set; }

    /// <summary>
    /// Patient's medical record code
    /// </summary>
    public required string MedicalRecordCode { get; set; }

    /// <summary>
    /// Patient's health insurance card number
    /// </summary>
    public required string HealthInsuranceCardNumber { get; set; }

    /// <summary>
    /// Patient's address
    /// </summary>
    public required string Address { get; set; }
}