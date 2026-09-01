using Respira.ServiceDefaults.Contracts.CQRS;

namespace Application.Features.Doctors.Rollback.Commands.CreateDoctor
{
    /// <summary>
    /// Compensates a failed CreateDoctor step by hard-deleting the created doctor profile.
    /// </summary>
    public record RollbackCreateDoctorCommand : ICommand
    {
        /// <summary>Saga identifier coordinating the rollback</summary>
        public required Guid SagaId { get; init; }

        /// <summary>Identifier of the doctor profile to remove</summary>
        public required Guid DoctorId { get; init; }
    }
}
