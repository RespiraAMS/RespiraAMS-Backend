using Respira.Authentication.Domain.Enums;
using Wolverine.Attributes;

namespace Respira.Authentication.Application.Features.Accounts.Commands.Update.Events
{
    [MessageIdentity("update-account-completed")]
    public class UpdateAccountCompleted
    {
        public Guid SagaId { get; set; }
        public Guid AccountId { get; set; }
        public required string OldEmail { get; set; }
        public required string OldPassword { get; set; }
        public required string OldPhone { get; set; }
        public RoleType OldRole { get; set; }
        public StatusType OldStatus { get; set; }
    }
}
