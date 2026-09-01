using Respira.ServiceDefaults.Contracts.CQRS;

namespace Application.Features.Authentication.Logout;

/// <summary>
/// Command to revoke the access and refresh tokens of the current session.
/// Both tokens are added to the blacklist so they can no longer be used.
/// </summary>
public record LogoutCommand : ICommand
{
    /// <summary>
    /// Raw access token to revoke
    /// </summary>
    public required string AccessToken { get; init; }

    /// <summary>
    /// Raw refresh token to revoke
    /// </summary>
    public required string RefreshToken { get; init; }
}
