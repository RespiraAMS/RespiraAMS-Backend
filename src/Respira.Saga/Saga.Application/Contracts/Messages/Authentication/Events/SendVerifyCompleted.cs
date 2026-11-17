using Wolverine.Attributes;

namespace Respira.Saga.Application.Contracts.Messages.Authentication.Events
{
    /// <summary>
    /// Published when the verification email has been handed to the SMTP service.
    /// </summary>
    [MessageIdentity("send-verify-completed")]
    public record SendVerifyCompleted
    {
        public SendVerifyCompleted(Guid sagaId, string email)
        {
            SagaId = sagaId;
            Email = email;
        }

        /// <summary>Identifier of the create saga waiting for this event</summary>
        public Guid SagaId { get; set; }

        /// <summary>Address the verification email was sent to</summary>
        public string Email { get; set; }
    }
}
