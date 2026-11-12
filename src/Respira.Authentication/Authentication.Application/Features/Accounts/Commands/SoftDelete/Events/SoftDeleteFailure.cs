using Wolverine.Attributes;

namespace Respira.Authentication.Application.Features.Accounts.Commands.SoftDelete.Events
{
    /// <summary>
    /// Published when the account could not be soft-deleted, leaving the saga partially
    /// applied and requiring operator attention.
    /// </summary>
    [MessageIdentity("soft-delete-failure")]
    public record SoftDeleteFailure
    {
        public SoftDeleteFailure(Guid sagaId, string reason)
        {
            SagaId = sagaId;
            Reason = reason;
        }

        /// <summary>Identifier of the create saga waiting for this event</summary>
        public Guid SagaId { get; set; }

        /// <summary>Why the compensation failed</summary>
        public string? Reason { get; set; }
    }
}
