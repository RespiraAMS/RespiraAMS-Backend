namespace Application.Abstracts.Email
{
    /// <summary>
    /// SMTP configuration for sending emails
    /// </summary>
    public class EmailOption
    {
        /// <summary>
        /// SMTP server hostname
        /// </summary>
        public required string Host { get; set; }

        /// <summary>
        /// SMTP server port
        /// </summary>
        public required int Port { get; set; }

        /// <summary>
        /// SMTP username (also used as the sender address)
        /// </summary>
        public required string Username { get; set; }

        /// <summary>
        /// SMTP password / app password
        /// </summary>
        public required string Password { get; set; }

        /// <summary>
        /// True to connect over SSL/TLS. Defaults to true
        /// </summary>
        public bool EnableSsl { get; set; } = true;
    }
}