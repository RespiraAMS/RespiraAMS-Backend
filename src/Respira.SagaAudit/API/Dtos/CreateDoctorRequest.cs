using Domain.Enums;

namespace Respira.SagaAudit.API.Dtos;

/// <summary>HTTP request body for creating a doctor on behalf of a manager/admin doctor.</summary>
public record CreateDoctorRequest
{
    /// <summary>Id of the manager/admin doctor initiating the creation.</summary>
    public required Guid ManagerDoctorId { get; init; }
    /// <summary>Email for the new auth account.</summary>
    public required string Email { get; init; }
    /// <summary>Initial password for the new auth account.</summary>
    public required string Password { get; init; }
    /// <summary>Phone number for the new auth account.</summary>
    public required string Phone { get; init; }
    /// <summary>Role assigned to the new auth account.</summary>
    public required RoleType Role { get; init; }

    /// <summary>Doctor's first name.</summary>
    public required string FirstName { get; init; }
    /// <summary>Doctor's last name.</summary>
    public required string LastName { get; init; }
    /// <summary>Degrees held by the doctor.</summary>
    public required List<DegreeType> Degrees { get; init; }
    /// <summary>Academic title of the doctor.</summary>
    public required AcademicTitleEnum AcademicTitle { get; init; }
    /// <summary>Position of the doctor.</summary>
    public required PositionType Position { get; init; }
    /// <summary>Gender of the doctor.</summary>
    public GenderType Gender { get; init; }
    /// <summary>Citizen identification number of the doctor.</summary>
    public required string CitizenIdentificationNumber { get; init; }
    /// <summary>Date of birth of the doctor, if provided.</summary>
    public DateTimeOffset? DateOfBirth { get; init; }
    /// <summary>Address of the doctor.</summary>
    public required string Address { get; init; }
}
