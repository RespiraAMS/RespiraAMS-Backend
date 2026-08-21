using Respira.ServiceDefaults.Constracts.CQRS;

namespace Application.Features.Tokens.Commands.RemoveToken
{
    /// <summary>
    /// Command to revoke a token: it is moved to the blacklist and removed
    /// from the issued tokens table.
    /// </summary>
    public class RemoveTokenCommand : ICommand
    {
        /// <summary>Raw token to revoke (hashed before lookup)</summary>
        public required string Token { get; init; }

        /// <summary>Reason for revocation (e.g. "logout", "password_reset")</summary>
        public required string Reason { get; init; }
    }
}
