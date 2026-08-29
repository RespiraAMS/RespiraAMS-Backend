namespace Application.Features.Authentication.GetListInfoDoctor.Results;

/// <summary>
/// Auth-side details of a single doctor, returned in bulk by
/// <see cref="GetListInfoDoctorQuery"/>.
/// </summary>
public record GetAuthDoctorListResult
{
    /// <summary>Doctor identifier (matches the requested id).</summary>
    public Guid Id { get; set; }

    /// <summary>Login email.</summary>
    public required string Email { get; set; }

    /// <summary>Contact phone number.</summary>
    public required string Phone { get; set; }

    /// <summary>Assigned role.</summary>
    public required string Role { get; set; }

    /// <summary>Whether the email has been confirmed.</summary>
    public bool IsEmailConfirmed { get; set; }

    /// <summary>Account status.</summary>
    public required string Status { get; set; }
}
