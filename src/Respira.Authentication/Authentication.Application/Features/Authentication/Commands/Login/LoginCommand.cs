using Respira.ServiceDefaults.Contracts.CQRS;

namespace Authentication.Application.Features.Authentication.Commands.Login
{
    public record LoginCommand : ICommand
    {
        public required string Email { get; set; }
        public required string Password { get; set; }
    }
}
