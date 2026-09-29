using Authentication.Domain.Enums;
using Respira.ServiceDefaults.Contracts.CQRS;

namespace Authentication.Application.Features.Accounts.Commands.Update
{
    public record UpdateAccountCommand : ICommand
    {
        public Guid SagaId { get; set; }
        public Guid AccountId { get; set; }
        public required string Email { get; set; }
        public required string Password { get; set; }
        public required string Phone { get; set; }
        public RoleType Role { get; set; }
        public StatusType Status { get; set; }
    }
}
