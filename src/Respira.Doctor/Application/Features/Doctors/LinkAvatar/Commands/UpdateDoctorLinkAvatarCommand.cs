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
        public required Guid SagaId { get; init; }
        public required Guid DoctorId { get; init; }
        public required Guid MediaId { get; init; }
    }
}
