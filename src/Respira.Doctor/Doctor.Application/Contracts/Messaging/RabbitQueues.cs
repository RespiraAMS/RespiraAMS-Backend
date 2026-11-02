namespace Respira.Doctor.Application.Constracts.Messaging
{
    /// <summary>
    /// RabbitMQ queue names shared (by convention, duplicated per service) across
    /// the create saga. Every service keeps its own copy of these constants.
    /// </summary>
    public static class RabbitQueues
    {
        /// <summary>Commands sent by the saga to this service.</summary>
        public const string DoctorCommands = "respira.doctor.commands";

        /// <summary>Queries sent by the Saga service to validate doctor ownership.</summary>
        public const string DoctorQueries = "respira.doctor.queries";

        /// <summary>Events this service publishes back to the saga.</summary>
        public const string SagaEvents = "respira.saga.events";

        /// <summary>Queue the authentication service listens on for account lookup queries.</summary>
        public const string AuthQueries = "respira.auth.queries";

        /// <summary>Queue the media service listens on for media lookup queries.</summary>
        public const string MediaQueries = "respira.media.queries";
    }
}
