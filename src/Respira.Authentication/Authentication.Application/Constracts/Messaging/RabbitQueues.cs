namespace Respira.Authentication.Application.Constracts.Messaging
{
    /// <summary>
    /// RabbitMQ queue names used by the create saga. Every participating service keeps
    /// its own copy of these constants: Wolverine routes messages by the CLR full type name,
    /// so the queue names are the only thing that has to be agreed out of band.
    /// </summary>
    public static class RabbitQueues
    {
        /// <summary>Queue this service listens on for commands sent by the saga.</summary>
        public const string AuthCommands = "respira.auth.commands";

        /// <summary>Queue the saga listens on for completion/failure events.</summary>
        public const string SagaEvents = "respira.saga.events";
    }
}
