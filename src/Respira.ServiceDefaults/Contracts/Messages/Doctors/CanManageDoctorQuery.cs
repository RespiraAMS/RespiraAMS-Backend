using Respira.ServiceDefaults.Contracts.CQRS;

namespace Respira.ServiceDefaults.Contracts.Messages.Doctors
{
    public record DoctorManagementAuthorization
    {
        public bool Allowed { get; init; }
        public Guid? MediaId { get; init; }
    }

    /// <summary>Checks that an actor is allowed to manage a doctor profile.</summary>
    public record CanManageDoctorQuery : IQuery
    {
        public Guid ActorAccountId { get; init; }
        public string ActorRole { get; init; } = string.Empty;
        public Guid DoctorId { get; init; }
        public Guid TargetAccountId { get; init; }
    }
}
