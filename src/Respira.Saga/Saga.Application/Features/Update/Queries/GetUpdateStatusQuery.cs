using Respira.Saga.Domain.Enums;
using Respira.ServiceDefaults.Contracts.CQRS;

namespace Respira.Saga.Application.Features.Update.Queries
{
    /// <summary>
    /// Reads the live state of a profile update run. Completed sagas are deleted by
    /// Wolverine, so a miss means the flow has already finished or never existed.
    /// </summary>
    public record GetUpdateStatusQuery : IQuery
    {
        public Guid SagaId { get; set; }
    }

    /// <summary>Snapshot of a profile update run that is still in flight.</summary>
    public record UpdateStatusResult
    {
        public Guid SagaId { get; set; }

        public UpdateStep Step { get; set; }

        public Guid AccountId { get; set; }

        public Guid DoctorId { get; set; }

        /// <summary>Identifier of the newly uploaded avatar.</summary>
        public Guid MediaId { get; set; }

        public string? FailureReason { get; set; }
    }
}
