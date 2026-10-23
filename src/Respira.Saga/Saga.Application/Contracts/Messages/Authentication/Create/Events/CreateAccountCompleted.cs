namespace Respira.Saga.Application.Constracts.Messages.Authentication.Create.Events
{
    /// <summary>
    /// Published when an account has been persisted, carrying the identifiers the
    /// create saga needs to continue with the next step.
    /// </summary>
    public record CreateAccountCompleted
    {
        /// <summary>Identifier of the create saga waiting for this event</summary>
        public Guid SagaId { get; set; }

        /// <summary>Identifier of the account that was created</summary>
        public Guid AccountId { get; set; }
    }
}
