using Domain.Enums;
using Respira.ServiceDefaults.Models;

namespace Domain.Models;

/*
 * Business rules:
 * 1. Patient is allow for update/delete only if that patient hasn't receive any treatment
 * 2. If patient has received treatment, only basic information that wouldn't affect
 * treatment can be updated (name, medical record code, health insurance, address, city, country)
 * 3. A patient without treatment cannot be discharge
 */

public class Patient : Base
{
    /// <summary>
    /// Patient full name
    /// </summary>
    public required string FullName { get; set => field = FullNameNormalize(value); }

    /// <summary>
    /// Patient date of birth
    /// </summary>
    public required DateOnly DateOfBirth { get; set; }

    /// <summary>
    /// Patient gender: true if male, false is female
    /// </summary>
    public required bool IsMale { get; set; }

    /// <summary>
    /// Patient medical record code. This code is from the hospital internal procedure that
    /// was assign to the patient
    /// </summary>
    public required string MedicalRecordCode { get; set; }

    /// <summary>
    /// Patient health insurance card number
    /// </summary>
    public required string HealthInsuranceCardNumber { get; set; }

    /// <summary>
    /// Patient address
    /// </summary>
    public required string Address { get; set; }

    public required string City { get; set; }

    public required string Country { get; set; }

    /// <summary>
    /// The time that patient was hospitalized
    /// </summary>
    public required DateTimeOffset Admission { get; set; }

    /// <summary>
    /// The time when patient can leave hospital (treatment ended)
    /// </summary>
    public required DateTimeOffset? Discharge { get; set; }

    /// <summary>
    /// Patient current status, like recovered, death,...
    /// </summary>
    public required PatientStatus Status { get; set; } = PatientStatus.InTreatment;

    /// <summary>
    /// Patient treatment timeline
    /// </summary>
    public List<Treatment> Treatments { get; set; } = [];

    public int Age()
    {
        var age = DateTimeOffset.UtcNow.Year - DateOfBirth.Year;
        if (DateOfBirth.AddYears(age) > DateOnly.FromDateTime(DateTime.UtcNow)) age--;
        return age;
    }

    public static string FullNameNormalize(string fullname)
    {
        fullname = fullname.Trim();

        return string.Join(" ", fullname
            .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)
            .Select(word => char.ToUpper(word[0]) + word[1..].ToLower())
        );
    }
}
