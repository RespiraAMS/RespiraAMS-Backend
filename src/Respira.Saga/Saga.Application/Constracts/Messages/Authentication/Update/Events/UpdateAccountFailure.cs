namespace Respira.Saga.Application.Constracts.Messages.Authentication.Update.Events
{
    public class UpdateAccountFailure
    {
        public Guid SagaId { get; set; }
        public Guid AccountId { get; set; }
        public required string Reason { get; set; }
    }
}
