using Wolverine.Attributes;

namespace Respira.Saga.Application.Contracts.Messages.Authentication.Update.Events
{
    [MessageIdentity("update-account-failure")]
    public class UpdateAccountFailure
    {
        public Guid SagaId { get; set; }
        public Guid AccountId { get; set; }
        public required string Reason { get; set; }
    }
}
