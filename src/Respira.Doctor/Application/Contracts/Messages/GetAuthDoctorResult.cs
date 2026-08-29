namespace Application.Contracts.Messages;

/// <summary>
/// Reply payload from the Auth service for a <see cref="GetUserQuery"/>.
/// </summary>
public record GetAuthDoctorResult
{
    public required string Email { get; set; }
    public required string Phone { get; set; }
    public required string Role { get; set; }
    public bool IsEmailConfirmed { get; set; }
    public required string Status { get; set; }
}
