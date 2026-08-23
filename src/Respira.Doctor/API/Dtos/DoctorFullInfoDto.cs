using Respira.Doctor.API.Clients;

namespace Respira.Doctor.API.Dtos;

/// <summary>
/// Complete doctor information combining data from Auth, Doctor, and Media services.
/// Used by admins to view full doctor details.
/// </summary>
public record DoctorFullInfoDto
{
    /// <summary>Doctor profile identifier</summary>
    public Guid Id { get; init; }

    /// <summary>Doctor first name</summary>
    public required string FirstName { get; init; }

    /// <summary>Doctor last name</summary>
    public required string LastName { get; init; }

    /// <summary>Full name (first + last)</summary>
    public required string FullName { get; init; }

    /// <summary>Academic degrees held by the doctor (as strings)</summary>
    public required List<string> Degrees { get; init; }

    /// <summary>Academic title (as a string)</summary>
    public required string AcademicTitle { get; init; }

    /// <summary>Hospital position (as a string)</summary>
    public required string Position { get; init; }

    /// <summary>Gender (as a string)</summary>
    public required string Gender { get; init; }

    /// <summary>Vietnam citizen identification number (CCCD/CMND)</summary>
    public required string CitizenIdentificationNumber { get; init; }

    /// <summary>Date of birth (if provided)</summary>
    public DateTimeOffset? DateOfBirth { get; init; }

    /// <summary>Residential address</summary>
    public required string Address { get; init; }

    /// <summary>Login email (from Auth)</summary>
    public required string Email { get; init; }

    /// <summary>Contact phone (from Auth)</summary>
    public required string Phone { get; init; }

    /// <summary>Account role (from Auth)</summary>
    public required string Role { get; init; }

    /// <summary>Whether the email is confirmed (from Auth)</summary>
    public bool IsEmailConfirmed { get; init; }

    /// <summary>Account status (from Auth)</summary>
    public required string Status { get; init; }

    /// <summary>Account creation timestamp (from Auth)</summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>Avatar URL (from Media, if linked)</summary>
    public string? AvatarUrl { get; init; }

    /// <summary>Avatar file name (from Media, if linked)</summary>
    public string? AvatarFileName { get; init; }
}

/// <summary>
/// Summary doctor info for list views.
/// </summary>
public record DoctorSummaryDto
{
    /// <summary>Doctor profile identifier</summary>
    public Guid Id { get; init; }

    /// <summary>Full name (first + last)</summary>
    public required string FullName { get; init; }

    /// <summary>Login email (from Auth, or "N/A")</summary>
    public required string Email { get; init; }

    /// <summary>Contact phone (from Auth, or "N/A")</summary>
    public required string Phone { get; init; }

    /// <summary>Hospital position (as a string)</summary>
    public required string Position { get; init; }

    /// <summary>Academic title (as a string)</summary>
    public required string AcademicTitle { get; init; }

    /// <summary>Account status (from Auth, or "Unknown")</summary>
    public required string Status { get; init; }

    /// <summary>Avatar URL (from Media, if linked)</summary>
    public string? AvatarUrl { get; init; }

    /// <summary>Profile creation timestamp</summary>
    public DateTimeOffset CreatedAt { get; init; }
}
