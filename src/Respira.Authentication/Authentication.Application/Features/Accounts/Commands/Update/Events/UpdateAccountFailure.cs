namespace Authentication.Application.Features.Accounts.Commands.Update.Events
{
    public class UpdateAccountFailure
    {
        public Guid SagaId { get; set; }
        public Guid AccountId { get; set; }
        public required string Reason { get; set; }
    }
}
