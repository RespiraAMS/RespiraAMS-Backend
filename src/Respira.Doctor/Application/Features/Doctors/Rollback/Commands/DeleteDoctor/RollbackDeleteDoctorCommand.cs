using Respira.ServiceDefaults.Contracts.CQRS;

namespace Application.Features.Doctors.Rollback.Commands.DeleteDoctor
{
    /// <summary>
    /// Compensates a failed DeleteDoctor step by restoring the soft-deleted doctor profile.
    /// </summary>
    public record RollbackDeleteDoctorCommand : ICommand
    {
        /// <summary>Saga identifier coordinating the rollback</summary>
        public required Guid SagaId { get; init; }

        /// <summary>Identifier of the doctor profile to restore</summary>
        public required Guid DoctorId { get; init; }

        /// <summary>Identifier of the doctor's creator (to re-attach as subordinate)</summary>
        public required Guid DoctorCreatorId { get; init; }
    }
}
