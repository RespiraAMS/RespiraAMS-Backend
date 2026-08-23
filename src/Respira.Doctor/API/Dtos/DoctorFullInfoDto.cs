using Respira.Doctor.API.Clients;

namespace Respira.Doctor.API.Dtos;

/// <summary>
/// Complete doctor information combining data from Auth, Doctor, and Media services.
/// Used by admins to view full doctor details.
/// </summary>
public record DoctorFullInfoDto
{
    // Doctor profile info
    public Guid Id { get; init; }
    public required string FirstName { get; init; }
    public required string LastName { get; init; }
    public required string FullName { get; init; }
    public required List<string> Degrees { get; init; }
    public required string AcademicTitle { get; init; }
    public required string Position { get; init; }
    public required string Gender { get; init; }
    public required string CitizenIdentificationNumber { get; init; }
    public DateTimeOffset? DateOfBirth { get; init; }
    public required string Address { get; init; }

    // Auth account info
    public required string Email { get; init; }
    public required string Phone { get; init; }
    public required string Role { get; init; }
    public bool IsEmailConfirmed { get; init; }
    public required string Status { get; init; }
    public DateTimeOffset CreatedAt { get; init; }

    // Media/Avatar info
    public string? AvatarUrl { get; init; }
    public string? AvatarFileName { get; init; }
}

/// <summary>
/// Summary doctor info for list views.
/// </summary>
public record DoctorSummaryDto
{
    public Guid Id { get; init; }
    public required string FullName { get; init; }
    public required string Email { get; init; }
    public required string Phone { get; init; }
    public required string Position { get; init; }
    public required string AcademicTitle { get; init; }
    public required string Status { get; init; }
    public string? AvatarUrl { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
}
