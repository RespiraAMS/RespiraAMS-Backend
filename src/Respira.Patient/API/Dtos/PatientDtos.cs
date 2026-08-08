using Application.Features.Patients.DischargePatient;
using Application.Features.Patients.GetPagedPatient;
using Application.Features.Patients.UpdatePatient;
using Domain.Enums;
using ImTools;
using Respira.ServiceDefaults.Dtos;

namespace Respira.Patient.API.Dtos;

public class GetPagedPatientRequestDto
{
    /// <summary>
    /// Pagination parameter: page index (1-based)
    /// </summary>
    public int Page { get; set; } = 1;

    /// <summary>
    /// Pagination parameter: page size
    /// </summary>
    public int Size { get; set; } = 10;

    /// <summary>
    /// Patient's fullname
    /// </summary>
    public string? FullName { get; set; }

    /// <summary>
    /// Patient's medical record code
    /// </summary>
    public string? MedicalRecordCode { get; set; }

    public GetPagedPatientQuery ToQuery()
    {
        return new GetPagedPatientQuery
        {
            Param = new PaginationParam
            {
                Page = Page,
                Size = Size
            },
            Filter = new PatientFilter()
            {
                FullName = FullName,
                MedicalRecordCode = MedicalRecordCode
            }
        };
    }
}

public class UpdatePatientRequestDto
{
    /// <summary>
    /// Patient's fullname
    /// </summary>
    public required string FullName { get; set; }

    /// <summary>
    /// Patient's date of birth
    /// </summary>
    public required DateOnly DateOfBirth { get; set; }

    /// <summary>
    /// Patient's gender: true if male, false if female
    /// </summary>
    public required bool IsMale { get; set; }

    /// <summary>
    /// Patient's medical record code in the internal hospital system
    /// </summary>
    public required string MedicalRecordCode { get; set; }

    /// <summary>
    /// Patient's health insurance card number (only accept the new 10 digits one)
    /// </summary>
    public required string HealthInsuranceCardNumber { get; set; }

    /// <summary>
    /// Patient's address
    /// </summary>
    public required string Address { get; set; }

    public UpdatePatientCommand ToCommand(Guid id)
    {
        return new UpdatePatientCommand
        {
            Id = id,
            FullName = FullName,
            DateOfBirth = DateOfBirth,
            IsMale = IsMale,
            MedicalRecordCode = MedicalRecordCode,
            HealthInsuranceCardNumber = HealthInsuranceCardNumber,
            Address = Address
        };
    }
}

public class DischargePatientRequestDto
{
    /// <summary>
    /// Patient's status
    /// </summary>
    public required PatientStatus Status { get; set; }

    public DischargePatientCommand ToCommand(Guid id)
    {
        return new DischargePatientCommand
        {
            Id = id,
            Status = Status
        };
    }
}