using Respira.Authentication.Domain.Enums;
using Respira.ServiceDefaults.Contracts.CQRS;

namespace Respira.Authentication.Application.Features.Accounts.Commands.ChangeStatus
{
    /// <summary>
    /// Command to change the status (Active/Inactive) of a specific account.
    /// Used directly by administrators to lock or unlock accounts without going through the profile saga.
    /// </summary>
    public record ChangeAccountStatusCommand : ICommand
    {
        /// <summary>The identifier of the account to lock or unlock.</summary>
        public Guid TargetAccountId { get; init; }

        /// <summary>The target status: Active (unlock) or Inactive (lock).</summary>
        public StatusType Status { get; init; }

        /// <summary>Identifier of the administrator executing the change.</summary>
        public Guid? ActorAccountId { get; init; }

        /// <summary>Role of the actor executing the change.</summary>
        public string? ActorRole { get; init; }
    }
}
