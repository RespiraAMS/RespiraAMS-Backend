namespace Respira.Saga.Application.Contracts.Messaging
{
    /// <summary>
    /// RabbitMQ queue names used by the profile creation saga. Every participating service keeps
    /// its own copy of these constants and pairs its message contracts via <c>[MessageIdentity]</c>.
    /// </summary>
    public static class RabbitQueues
    {
        /// <summary>Queue the Authentication service listens on for commands.</summary>
        public const string AuthCommands = "respira.auth.commands";

        /// <summary>Queue the Doctor service listens on for commands.</summary>
        public const string DoctorCommands = "respira.doctor.commands";

        /// <summary>Queue for authorization queries handled by the Doctor service.</summary>
        public const string DoctorQueries = "respira.doctor.queries";

        /// <summary>Queue this service listens on for completion and failure events.</summary>
        public const string SagaEvents = "respira.saga.events";
    }
}
