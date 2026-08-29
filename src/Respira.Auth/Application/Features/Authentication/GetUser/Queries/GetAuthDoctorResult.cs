namespace Application.Features.Authentication.GetUser.Queries;

/// <summary>
/// Result of a <see cref="GetUserQuery"/>: the auth-side details of a doctor account.
/// </summary>
public record GetAuthDoctorResult
{
    public required string Email { get; set; }
    public required string Phone { get; set; }
    public required string Role { get; set; }
    public bool IsEmailConfirmed { get; set; }
    public required string Status { get; set; }
}
