namespace Respira.Media.Application.Contracts.Messaging
{
    /// <summary>
    /// RabbitMQ queue names used by the media service. Every participating service keeps
    /// its own copy of these constants: Wolverine routes messages by the CLR full type name,
    /// so the queue names are the only thing that has to be agreed out of band.
    /// </summary>
    public static class RabbitQueues
    {
        /// <summary>Queue this service listens on for media lookup queries.</summary>
        public const string MediaQueries = "respira.media.queries";
    }
}
