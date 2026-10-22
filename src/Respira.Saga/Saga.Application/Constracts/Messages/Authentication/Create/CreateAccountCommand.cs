using Respira.Authentication.Domain.Enums;
using Respira.ServiceDefaults.Contracts.CQRS;

namespace Respira.Saga.Application.Constracts.Messages.Authentication.Create
{
    public record CreateAccountCommand : ICommand
    {
        public Guid SagaId { get; set; }
        public required string Email { get; set; }
        public required string Password { get; set; }
        public required string Phone { get; set; }
        public RoleType Role { get; set; }
        public StatusType Status { get; set; }
    }
}
