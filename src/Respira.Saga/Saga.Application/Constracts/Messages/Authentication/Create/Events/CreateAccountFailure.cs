namespace Respira.Saga.Application.Constracts.Messages.Authentication.Create.Events
{
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
