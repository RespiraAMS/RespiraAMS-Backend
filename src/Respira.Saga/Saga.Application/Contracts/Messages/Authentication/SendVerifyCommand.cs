using Respira.ServiceDefaults.Contracts.CQRS;

namespace Respira.Saga.Application.Contracts.Messages.Authentication
{
    public record SendVerifyCommand : ICommand
    {
        public required string Email { get; init; }

        /// <summary>
        /// Create saga correlation. <see cref="Guid.Empty"/> when the request does not
        /// originate from the saga, in which case no completion event is published.
        /// </summary>
        public Guid SagaId { get; init; }

        public Guid ActorAccountId { get; init; }

        public string ActorRole { get; init; } = string.Empty;

        public Guid TargetAccountId { get; init; }
    }
}
