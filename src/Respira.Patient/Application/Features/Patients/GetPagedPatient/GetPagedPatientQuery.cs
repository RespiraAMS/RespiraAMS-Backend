using Domain.Enums;

namespace Application.Features.Patients.GetPagedPatient;

public class PatientFilter
{
    /// <summary>
    /// Patient's fullname
    /// </summary>
    public string? FullName { get; set; }

    /// <summary>
    /// Patient's medical record code
    /// </summary>
    public string? MedicalRecordCode { get; set; }
}

public class GetPagedPatientQuery : IQuery
{
    /// <summary>
    /// Pagination parameter
    /// </summary>
    public required PaginationParam Param { get; set; }

    /// <summary>
    /// Patient filter
    /// </summary>
    public PatientFilter? Filter { get; set; }
}

public class PagedPatientItem
{
    /// <summary>
    /// Patient's ID
    /// </summary>
    public required Guid Id { get; set; }

    /// <summary>
    /// Patient's full name
    /// </summary>
    public required string FullName { get; set; }

    public required int Age { get; set; }
    public required bool IsMale { get; set; }

    /// <summary>
    /// Patient's medical record code
    /// </summary>
    public required string MedicalRecordCode { get; set; }

    /// <summary>
    /// Patient's status
    /// </summary>
    public required PatientStatus Status { get; set; }
}
