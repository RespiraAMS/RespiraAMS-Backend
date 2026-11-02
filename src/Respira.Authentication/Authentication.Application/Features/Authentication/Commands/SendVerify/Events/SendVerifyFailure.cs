namespace Respira.Authentication.Application.Features.Authentication.Commands.SendVerify.Events
{
    /// <summary>
    /// Published when the verification email could not be issued or delivered.
    /// </summary>
    public record SendVerifyFailure
    {
        public SendVerifyFailure(Guid sagaId, string reason)
        {
            SagaId = sagaId;
            Reason = reason;
        }

        /// <summary>Identifier of the create saga waiting for this event</summary>
        public Guid SagaId { get; set; }

        /// <summary>Why the verification email was not sent</summary>
        public string? Reason { get; set; }
    }
}
