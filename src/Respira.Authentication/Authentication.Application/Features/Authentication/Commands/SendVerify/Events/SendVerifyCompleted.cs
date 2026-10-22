namespace Respira.Authentication.Application.Features.Authentication.Commands.SendVerify.Events
{
    /// <summary>
    /// Published when the verification email has been handed to the SMTP service.
    /// </summary>
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
