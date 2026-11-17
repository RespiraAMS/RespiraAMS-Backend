using Respira.Saga.Domain.Enums;
using Respira.ServiceDefaults.Contracts.CQRS;

namespace Respira.Saga.Application.Features.Create.Queries
{
    /// <summary>
    /// Reads the live state of a profile creation run. Completed sagas are deleted by Wolverine,
    /// so a miss means the flow has already finished or never existed.
    /// </summary>
    public record GetCreateStatusQuery : IQuery
    {
        public Guid SagaId { get; set; }
    }

    /// <summary>Snapshot of a profile creation run that is still in flight.</summary>
    public record CreateStatusResult
    {
        public Guid SagaId { get; set; }

        public CreateStep Step { get; set; }

        public Guid AccountId { get; set; }

        public Guid DoctorId { get; set; }

        public Guid MediaId { get; set; }

        public string? FailureReason { get; set; }
    }
}
