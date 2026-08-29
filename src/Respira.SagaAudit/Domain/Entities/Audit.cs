using Domain.Enums;
using Respira.ServiceDefaults.Models;

namespace Domain.Entities
{
    public class Audit : Base
    {
        public required string ServiceName { get; set; }
        public required string EntityName { get; set; }
        public Guid? UserCreatedId { get; set; }
        public AuditActionType ActionType { get; set; }
        public required string Data { get; set; }
    }
}
