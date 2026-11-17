using Respira.Authentication.Domain.Enums;
using Respira.ServiceDefaults.Contracts.CQRS;
using Wolverine.Attributes;

namespace Respira.Authentication.Application.Features.Accounts.Commands.Create
{
    [MessageIdentity("create-account-command")]
    public record CreateAccountCommand : ICommand
    {
        public Guid SagaId { get; set; }
        public Guid ActorAccountId { get; set; }
        public string ActorRole { get; set; } = string.Empty;
        public required string Email { get; set; }
        public required string Password { get; set; }
        public required string Phone { get; set; }
        public RoleType Role { get; set; }
        public StatusType Status { get; set; }
    }
}
