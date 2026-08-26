namespace Application.Contracts.Services
{
    public class MailServiceOptions
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

    public interface IEmailService
    {
        Task SendEmailAsync(IEnumerable<(string Name, string Email)> recipients, string subject, string body, (string filename, byte[] data)? attachment);
    }
}
