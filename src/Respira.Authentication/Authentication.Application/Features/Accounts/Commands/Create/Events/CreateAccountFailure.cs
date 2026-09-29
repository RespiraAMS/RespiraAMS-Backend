namespace Authentication.Application.Features.Accounts.Commands.Create.Events
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
