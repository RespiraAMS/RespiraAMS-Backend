using Respira.Saga.Domain.Enums;
using Respira.ServiceDefaults.Contracts.CQRS;

namespace Respira.Saga.Application.Features.Delete.Queries
{
    /// <summary>
    /// Reads the live state of a profile deletion run. Completed sagas are deleted by
    /// Wolverine, so a miss means the flow has already finished or never existed.
    /// </summary>
    public record GetDeleteStatusQuery : IQuery
    {
        public Guid SagaId { get; set; }
    }

    /// <summary>Snapshot of a profile deletion run that is still in flight.</summary>
    public record DeleteStatusResult
    {
        public Guid SagaId { get; set; }

        public DeleteStep Step { get; set; }

        public Guid AccountId { get; set; }

        public Guid DoctorId { get; set; }

        public Guid MediaId { get; set; }

        public string? FailureReason { get; set; }
    }
}
