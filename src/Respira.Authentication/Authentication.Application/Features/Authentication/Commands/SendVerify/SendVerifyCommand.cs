using Respira.ServiceDefaults.Contracts.CQRS;

namespace Respira.Authentication.Application.Features.Authentication.Commands.SendVerify
{
    public record SendVerifyCommand : ICommand
    {
        public required string Email { get; init; }

        /// <summary>
        /// Create saga correlation. <see cref="Guid.Empty"/> when the request does not
        /// originate from the saga, in which case no completion event is published.
        /// </summary>
        public Guid SagaId { get; init; }
    }
}
