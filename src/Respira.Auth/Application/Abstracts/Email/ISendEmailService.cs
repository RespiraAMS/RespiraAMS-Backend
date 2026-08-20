namespace Application.Abstracts.Email
{
    /// <summary>
    /// Abstraction over an email sending provider.
    /// </summary>
    public interface ISendEmailService
    {
        /// <summary>Sends an email to a single recipient</summary>
        /// <param name="to">Recipient email address</param>
        /// <param name="subject">Email subject</param>
        /// <param name="body">Email body (HTML)</param>
        public Task SendEmailAsync(string to, string subject, string body);
    }
}
