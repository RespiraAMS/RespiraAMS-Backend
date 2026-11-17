using Respira.Doctor.Domain.Entities;
using Respira.Doctor.Domain.Enums;
using DoctorEntity = Respira.Doctor.Domain.Entities.Doctor;

namespace Respira.Doctor.Application.Test.TestData;

/// <summary>
/// Builds doctor and head of department doctor profiles for the tests.
/// </summary>
public static class DoctorFactory
{
    public static DoctorEntity CreateDoctor(
        Guid? accountId = null,
        string firstName = "Nguyen",
        string lastName = "Van A",
        string citizenIdentificationNumber = "001088012345",
        Guid? headDepartmentDoctorId = null,
        Guid? mediaId = null,
        GenderType gender = GenderType.Male,
        AcademicTitle academicTitle = AcademicTitle.None,
        PositionType position = PositionType.StaffDoctor,
        DateTimeOffset? dateOfBirth = null,
        DateTimeOffset? createdAt = null,
        bool isDeleted = false
    )
    {
        var doctor = new DoctorEntity
        {
            AccountId = accountId ?? Guid.CreateVersion7(),
            FirstName = firstName,
            LastName = lastName,
            Degrees = [DegreeType.Doctor, DegreeType.Master],
            AcademicTitle = academicTitle,
            Position = position,
            Gender = gender,
            CitizenIdentificationNumber = citizenIdentificationNumber,
            DateOfBirth = dateOfBirth,
            Address = "12 Le Loi, District 1, Ho Chi Minh City",
            HeadDepartmentDoctorId = headDepartmentDoctorId,
            MediaId = mediaId,
        };

        if (createdAt.HasValue)
        {
            doctor.CreatedAt = createdAt.Value;
            doctor.UpdatedAt = createdAt.Value;
        }

        if (isDeleted)
        {
            doctor.IsDeleted = true;
            doctor.DeletedAt = createdAt ?? DateTimeOffset.UtcNow;
        }

        return doctor;
    }

    public static HeadDepartmentDoctor CreateHeadDepartmentDoctor(
        Guid? accountId = null,
        string firstName = "Tran",
        string lastName = "Thi B",
        string citizenIdentificationNumber = "001088054321",
        Guid? mediaId = null,
        GenderType gender = GenderType.Female,
        AcademicTitle academicTitle = AcademicTitle.Professor,
        PositionType position = PositionType.HeadOfDepartment,
        DateTimeOffset? dateOfBirth = null,
        DateTimeOffset? createdAt = null,
        bool isDeleted = false
    )
    {
        var head = new HeadDepartmentDoctor
        {
            AccountId = accountId ?? Guid.CreateVersion7(),
            FirstName = firstName,
            LastName = lastName,
            Degrees = [DegreeType.Doctor, DegreeType.PhD],
            AcademicTitle = academicTitle,
            Position = position,
            Gender = gender,
            CitizenIdentificationNumber = citizenIdentificationNumber,
            DateOfBirth = dateOfBirth,
            Address = "45 Le Loi, District 1, Ho Chi Minh City",
            MediaId = mediaId,
        };

        if (createdAt.HasValue)
        {
            head.CreatedAt = createdAt.Value;
            head.UpdatedAt = createdAt.Value;
        }

        if (isDeleted)
        {
            head.IsDeleted = true;
            head.DeletedAt = createdAt ?? DateTimeOffset.UtcNow;
        }

        return head;
    }
}
