using Domain.Enums;

namespace Respira.SagaAudit.API.Dtos;

/// <summary>
/// HTTP request body for updating a doctor on behalf of a manager/admin doctor.
/// Both the new values (to apply) and the old values (to compensate) are required.
/// </summary>
public record UpdateDoctorRequest
{
    public required Guid ManagerDoctorId { get; init; }
    public required Guid AuthUserId { get; init; }
    public required Guid DoctorId { get; init; }
    public required Guid MediaId { get; init; }

    public required string Email { get; init; }
    public required string Phone { get; init; }
    public required RoleType Role { get; init; }

    public required string OldEmail { get; init; }
    public required string OldPhone { get; init; }
    public required RoleType OldRole { get; init; }

    public required string FirstName { get; init; }
    public required string LastName { get; init; }
    public required List<DegreeType> Degrees { get; init; }
    public required AcademicTitleEnum AcademicTitle { get; init; }
    public required PositionType Position { get; init; }
    public GenderType Gender { get; init; }
    public required string CitizenIdentificationNumber { get; init; }
    public DateTimeOffset? DateOfBirth { get; init; }
    public required string Address { get; init; }

    public required string OldFirstName { get; init; }
    public required string OldLastName { get; init; }
    public required List<DegreeType> OldDegrees { get; init; }
    public required AcademicTitleEnum OldAcademicTitle { get; init; }
    public required PositionType OldPosition { get; init; }
    public GenderType OldGender { get; init; }
    public required string OldCitizenIdentificationNumber { get; init; }
    public DateTimeOffset? OldDateOfBirth { get; init; }
    public required string OldAddress { get; init; }

    public bool HasNewMedia { get; init; }
    public string? NewMediaFileName { get; init; }
    public string? NewMediaContentType { get; init; }
    public long NewMediaSize { get; init; }
    public byte[]? NewMediaData { get; init; }
}

public record DeleteDoctorRequest
{
    public required Guid ManagerDoctorId { get; init; }
    public required Guid AuthUserId { get; init; }
    public required Guid DoctorId { get; init; }
    public required Guid MediaId { get; init; }
}
