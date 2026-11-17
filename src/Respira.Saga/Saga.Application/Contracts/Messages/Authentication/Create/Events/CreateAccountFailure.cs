using Wolverine.Attributes;

namespace Respira.Saga.Application.Contracts.Messages.Authentication.Create.Events
{
    [MessageIdentity("create-account-failure")]
    public record CreateAccountFailure
    {
        public CreateAccountFailure(Guid sagaId, string reason)
        {
            SagaId = sagaId;
            Reason = reason;
        }

        public Guid SagaId { get; set; }
        public string? Reason { get; set; }
    }
}
