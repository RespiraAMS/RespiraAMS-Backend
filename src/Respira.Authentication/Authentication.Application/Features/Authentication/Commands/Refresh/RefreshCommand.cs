using Respira.ServiceDefaults.Contracts.CQRS;

namespace Authentication.Application.Features.Authentication.Commands.Refresh
{
    /// <summary>
    /// Requests a new access token and rotates the supplied refresh token.
    /// </summary>
    public record RefreshCommand : ICommand
    {
        /// <summary>
        /// The raw refresh token previously issued to the account.
        /// </summary>
        public required string RefreshToken { get; init; }
    }
}
