using Respira.ServiceDefaults.Contracts.CQRS;

namespace Authentication.Application.Features.Authentication.Commands.SendVerify
{
    public record SendVerifyCommand : ICommand
    {
        public required string Email { get; init; }
    }
}
