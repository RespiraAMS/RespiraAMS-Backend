using Domain.Enums;

namespace Respira.SagaAudit.API.Dtos;

/// <summary>
/// HTTP request body for updating a doctor on behalf of a manager/admin doctor.
/// Both the new values (to apply) and the old values (to compensate) are required.
/// </summary>
public record UpdateDoctorRequest
{
    /// <summary>Id of the manager/admin doctor initiating the update.</summary>
    public required Guid ManagerDoctorId { get; init; }

    /// <summary>The shared entity ID — same for both Auth and Doctor tables (AuthUserId == DoctorId).</summary>
    public required Guid EntityId { get; init; }

    /// <summary>Current avatar media id of the doctor.</summary>
    public required Guid MediaId { get; init; }

    /// <summary>New email to apply to the auth account.</summary>
    public required string Email { get; init; }
    /// <summary>New phone to apply to the auth account.</summary>
    public required string Phone { get; init; }
    /// <summary>New role to apply to the auth account.</summary>
    public required RoleType Role { get; init; }

    /// <summary>Previous email, used to compensate a failed auth update.</summary>
    public required string OldEmail { get; init; }
    /// <summary>Previous phone, used to compensate a failed auth update.</summary>
    public required string OldPhone { get; init; }
    /// <summary>Previous role, used to compensate a failed auth update.</summary>
    public required RoleType OldRole { get; init; }

    /// <summary>New first name to apply to the doctor profile.</summary>
    public required string FirstName { get; init; }
    /// <summary>New last name to apply to the doctor profile.</summary>
    public required string LastName { get; init; }
    /// <summary>New degrees to apply to the doctor profile.</summary>
    public required List<DegreeType> Degrees { get; init; }
    /// <summary>New academic title to apply to the doctor profile.</summary>
    public required AcademicTitleEnum AcademicTitle { get; init; }
    /// <summary>New position to apply to the doctor profile.</summary>
    public required PositionType Position { get; init; }
    /// <summary>New gender to apply to the doctor profile.</summary>
    public GenderType Gender { get; init; }
    /// <summary>New citizen identification number to apply to the doctor profile.</summary>
    public required string CitizenIdentificationNumber { get; init; }
    /// <summary>New date of birth to apply to the doctor profile, if provided.</summary>
    public DateTimeOffset? DateOfBirth { get; init; }
    /// <summary>New address to apply to the doctor profile.</summary>
    public required string Address { get; init; }

    /// <summary>Previous first name, used to compensate a failed doctor update.</summary>
    public required string OldFirstName { get; init; }
    /// <summary>Previous last name, used to compensate a failed doctor update.</summary>
    public required string OldLastName { get; init; }
    /// <summary>Previous degrees, used to compensate a failed doctor update.</summary>
    public required List<DegreeType> OldDegrees { get; init; }
    /// <summary>Previous academic title, used to compensate a failed doctor update.</summary>
    public required AcademicTitleEnum OldAcademicTitle { get; init; }
    /// <summary>Previous position, used to compensate a failed doctor update.</summary>
    public required PositionType OldPosition { get; init; }
    /// <summary>Previous gender, used to compensate a failed doctor update.</summary>
    public GenderType OldGender { get; init; }
    /// <summary>Previous citizen identification number, used to compensate a failed doctor update.</summary>
    public required string OldCitizenIdentificationNumber { get; init; }
    /// <summary>Previous date of birth, used to compensate a failed doctor update, if provided.</summary>
    public DateTimeOffset? OldDateOfBirth { get; init; }
    /// <summary>Previous address, used to compensate a failed doctor update.</summary>
    public required string OldAddress { get; init; }

    /// <summary>When true, the avatar is replaced with the media uploaded via the Media upload endpoint.</summary>
    public bool HasNewMedia { get; init; }
    /// <summary>Id of the newly uploaded avatar media, when <see cref="HasNewMedia"/> is true.</summary>
    public Guid? NewMediaId { get; init; }
}

/// <summary>HTTP request body for deleting a doctor on behalf of a manager/admin doctor.</summary>
public record DeleteDoctorRequest
{
    /// <summary>Id of the manager/admin doctor initiating the deletion.</summary>
    public required Guid ManagerDoctorId { get; init; }

    /// <summary>The shared entity ID — same for both Auth and Doctor tables.</summary>
    public required Guid EntityId { get; init; }

    /// <summary>Avatar media id to remove.</summary>
    public required Guid MediaId { get; init; }
}
