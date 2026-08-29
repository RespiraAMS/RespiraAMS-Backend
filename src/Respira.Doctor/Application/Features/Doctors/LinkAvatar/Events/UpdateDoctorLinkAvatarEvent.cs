namespace Application.Features.Doctors.LinkAvatar.Events
{
    /// <summary>
    /// Published when an avatar is successfully linked to a doctor profile in the UpdateDoctor flow.
    /// </summary>
    public record UpdateDoctorLinkAvatarSuccessEvent
    {
        /// <summary>Saga identifier</summary>
        public required Guid SagaId { get; init; }

        /// <summary>Identifier of the doctor profile</summary>
        public required Guid DoctorId { get; init; }

        /// <summary>Identifier of the linked avatar media</summary>
        public Guid? MediaId { get; init; }
    }

    /// <summary>
    /// Published when linking an avatar to a doctor profile fails in the UpdateDoctor flow.
    /// </summary>
    public record UpdateDoctorLinkAvatarFailureEvent
    {
        /// <summary>Saga identifier</summary>
        public required Guid SagaId { get; init; }

        /// <summary>Identifier of the doctor profile</summary>
        public required Guid DoctorId { get; init; }
    }
}
