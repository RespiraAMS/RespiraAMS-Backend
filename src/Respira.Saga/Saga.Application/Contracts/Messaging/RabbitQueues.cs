namespace Respira.Saga.Application.Constracts.Messaging
{
    /// <summary>
    /// RabbitMQ queue names used by the profile creation saga. Every participating service keeps
    /// its own copy of these constants: they are the only thing that has to be agreed out
    /// band, since message types are matched by their CLR full type name.
    /// </summary>
    public static class RabbitQueues
    {
        /// <summary>Queue the Authentication service listens on for commands.</summary>
        public const string AuthCommands = "respira.auth.commands";

        /// <summary>Queue the Doctor service listens on for commands.</summary>
        public const string DoctorCommands = "respira.doctor.commands";

        /// <summary>Queue this service listens on for completion and failure events.</summary>
        public const string SagaEvents = "respira.saga.events";
    }
}
