using Respira.ServiceDefaults.Constracts.CQRS;

namespace Application.Features.Doctors.Delete.Commands
{
    /// <summary>
    /// Soft-deletes a doctor profile. Part of the DeleteDoctor saga.
    /// </summary>
    public record DeleteDoctorCommand : ICommand
    {
        /// <summary>Saga identifier coordinating the doctor deletion</summary>
        public required Guid SagaId { get; init; }

        /// <summary>Identifier of the doctor profile to delete</summary>
        public required Guid DoctorId { get; init; }

        /// <summary>Identifier of the doctor who created this profile (if any)</summary>
        public Guid? DoctorCreatorId { get; init; }
    }
}
