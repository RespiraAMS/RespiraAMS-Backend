namespace Authentication.Application.Features.Accounts.Commands.SoftDelete.Events
{
    /// <summary>
    /// Published when the account has been soft-deleted. Compensation is idempotent:
    /// an account that is already gone also reports completion.
    /// </summary>
    public record SoftDeleteCompleted
    {
        public SoftDeleteCompleted(Guid sagaId, Guid accountId)
        {
            SagaId = sagaId;
            AccountId = accountId;
        }

        /// <summary>Identifier of the create saga waiting for this event</summary>
        public Guid SagaId { get; set; }

        /// <summary>Account that was soft-deleted</summary>
        public Guid AccountId { get; set; }
    }
}
