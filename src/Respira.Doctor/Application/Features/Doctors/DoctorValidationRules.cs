namespace Application.Features.Doctors;

/// <summary>
/// Shared validation rules for doctor commands.
/// </summary>
public static class DoctorValidationRules
{
    /// <summary>
    /// Vietnam Citizen Identification Number (CCCD/CMND): 9-digit old ID (CMND)
    /// or 12-digit new chip-based ID (CCCD), numeric only.
    /// </summary>
    public const string CitizenIdentificationNumberPattern = @"^(\d{9}|\d{12})$";
}
