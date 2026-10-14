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

        /// <summary>Events this service publishes back to the saga.</summary>
        public const string SagaEvents = "respira.saga.events";
    }
}
