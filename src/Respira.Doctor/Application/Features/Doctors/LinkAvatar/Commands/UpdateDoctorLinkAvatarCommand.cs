using Respira.ServiceDefaults.Constracts.CQRS;

namespace Application.Features.Doctors.LinkAvatar.Commands
{
    /// <summary>
    /// Links an already-uploaded avatar media to a doctor profile as part of the
    /// UpdateDoctor saga. Uses a saga-specific event pair so it does not collide with the
    /// CreateDoctor saga's <see cref="LinkDoctorAvatarCommand"/> flow.
    /// </summary>
    public record UpdateDoctorLinkAvatarCommand : ICommand
    {
        /// <summary>Saga identifier coordinating the avatar link</summary>
        public required Guid SagaId { get; init; }

        /// <summary>Identifier of the doctor profile to link the avatar to</summary>
        public required Guid DoctorId { get; init; }

        /// <summary>Identifier of the uploaded avatar media</summary>
        public required Guid MediaId { get; init; }
    }
}
