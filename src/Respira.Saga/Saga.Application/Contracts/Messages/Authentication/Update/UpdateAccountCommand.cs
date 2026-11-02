using Respira.Authentication.Domain.Enums;
using Respira.ServiceDefaults.Contracts.CQRS;

namespace Respira.Saga.Application.Contracts.Messages.Authentication.Update
{
    /// <summary>
    /// Updates the mutable fields of an account. Everything but the identifiers and the
    /// contact data is optional, so a profile-only change never resets the password, the
    /// role or the status of the account.
    /// </summary>
    public record UpdateAccountCommand : ICommand
    {
        public Guid SagaId { get; set; }
        public Guid ActorAccountId { get; set; }
        public string ActorRole { get; set; } = string.Empty;
        public Guid AccountId { get; set; }
        public required string Email { get; set; }
        public required string Phone { get; set; }

        /// <summary>New plaintext password, or null to keep the current one.</summary>
        public string? Password { get; set; }

        /// <summary>New role, or null to keep the current one.</summary>
        public RoleType? Role { get; set; }

        /// <summary>New status, or null to keep the current one.</summary>
        public StatusType? Status { get; set; }
    }
}
