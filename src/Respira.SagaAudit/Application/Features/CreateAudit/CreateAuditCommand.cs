using Domain.Enums;
using Respira.ServiceDefaults.Constracts.CQRS;

namespace Application.Features.CreateAudit
{
    public record CreateAuditCommand : ICommand
    {
        public required string ServiceName { get; set; }
        public required string EntityName { get; set; }
        public Guid? UserCreatedId { get; set; }
        public AuditActionType ActionType { get; set; }
        public required string Data { get; set; }
    }
}
