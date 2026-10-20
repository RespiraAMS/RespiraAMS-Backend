using Respira.ServiceDefaults.Contracts.CQRS;

namespace Authentication.Application.Features.Authentication.Commands.Logout
{
    public record LogoutCommand : ICommand
    {
        public required string AccessToken { get; init; }
        public required string RefreshToken { get; init; }
    }
}
