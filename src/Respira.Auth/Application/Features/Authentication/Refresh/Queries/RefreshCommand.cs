using Respira.ServiceDefaults.Constracts.CQRS;

namespace Application.Features.Authentication.Refresh.Queries;

/// <summary>
/// Query to obtain a fresh access (and refresh) token pair using a valid refresh token.
/// The refresh token is validated, rotated and persisted; the previous refresh token is revoked.
/// </summary>
public record RefreshCommand : IQuery
{
    /// <summary>Raw refresh token issued during login</summary>
    public required string RefreshToken { get; init; }
}
