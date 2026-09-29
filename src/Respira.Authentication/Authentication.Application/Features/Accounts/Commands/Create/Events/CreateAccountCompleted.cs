namespace Authentication.Application.Features.Accounts.Commands.Create.Events
{
    public record CreateAccountCompleted
    {
        public Guid SagaId { get; set; }
    }
}
