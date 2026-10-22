using Respira.ServiceDefaults.Contracts.CQRS;

namespace Respira.Authentication.Application.Features.Accounts.Commands.SoftDelete
{
    /// <summary>
    /// Soft-deletes an account. Used by the create saga as the compensation for
    /// every step that failed after the account was created.
    /// </summary>
    public record SoftDeleteCommand : ICommand
    {
        /// <summary>Identifier of the create saga requesting the compensation</summary>
        public Guid SagaId { get; set; }

        /// <summary>Account to soft-delete</summary>
        public Guid AccountId { get; set; }
    }
}
