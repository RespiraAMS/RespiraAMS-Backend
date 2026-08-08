namespace Application.Features.Patients.CreatePatient;

public class CreatePatientCommand : ICommand
{
    /// <summary>
    /// Patient's full name. This value will be automatically normalize into Title case (capitalized)
    /// </summary>
    public required string FullName { get; set; }

    /// <summary>
    /// Patient's date of birth
    /// </summary>
    public required DateOnly DateOfBirth { get; set; }

    /// <summary>
    /// Patient's gender. True if male, false if female
    /// </summary>
    public required bool IsMale { get; set; }

    /// <summary>
    /// Patient's medical record code in the internal hospital system
    /// </summary>
    public required string MedicalRecordCode { get; set; }

    /// <summary>
    /// Patient's health insurance card number. Only accept the new card (10 digits), not the old one (15 digits) 
    /// </summary>
    public required string HealthInsuranceCardNumber { get; set; }

    /// <summary>
    /// Patient's address
    /// </summary>
    public required string Address { get; set; }
}

public class CreatePatientResult(Guid id)
{
    /// <summary>
    /// Patient's ID
    /// </summary>
    public Guid Id { get; set; } = id;
}