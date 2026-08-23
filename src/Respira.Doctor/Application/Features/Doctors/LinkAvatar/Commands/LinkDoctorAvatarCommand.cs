using Respira.ServiceDefaults.Constracts.CQRS;

namespace Application.Features.Doctors.LinkAvatar.Commands
{
    /// <summary>
    /// Links an already-uploaded avatar media to a doctor profile. Used by the
    /// CreateDoctor saga's final step so its completion event does not collide with
    /// the <see cref="Application.Features.Doctors.Update.Commands.UpdateDoctorCommand"/> flow.
    /// </summary>
    public record LinkDoctorAvatarCommand : ICommand
    {
        public required Guid SagaId { get; init; }
        public required Guid DoctorId { get; init; }
        public required Guid MediaId { get; init; }
    }
}
