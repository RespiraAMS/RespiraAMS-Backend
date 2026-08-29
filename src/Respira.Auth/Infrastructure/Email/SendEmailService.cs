using Application.Abstracts.Email;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace Infrastructure.Email
{
    /// <summary>
    /// Sends emails over SMTP using MailKit
    /// </summary>
    /// <param name="emailOption">SMTP configuration (host, port, credentials, TLS)</param>
    public class SendEmailService(IOptions<EmailOption> emailOption) : ISendEmailService
    {
        /// <summary>
        /// Sends an HTML email to a single recipient
        /// </summary>
        /// <param name="to">Recipient email address</param>
        /// <param name="subject">Email subject</param>
        /// <param name="body">Email body (HTML)</param>
        public async Task SendEmailAsync(string to, string subject, string body)
        {
            var option = emailOption.Value;

            var message = new MimeMessage
            {
                From = { new MailboxAddress("Respira AMS", option.Username) },
                To = { MailboxAddress.Parse(to) },
                Subject = subject,
                Body = new BodyBuilder { HtmlBody = body }.ToMessageBody(),
            };

            using var client = new SmtpClient();
            var secureSocketOptions = option.Port switch
            {
                465 => SecureSocketOptions.SslOnConnect,
                587 or 25 or 2525 => SecureSocketOptions.StartTls,
                _ => option.EnableSsl
                    ? SecureSocketOptions.StartTls
                    : SecureSocketOptions.None,
            };
            await client.ConnectAsync(option.Host, option.Port, secureSocketOptions);
            await client.AuthenticateAsync(option.Username, option.Password);
            await client.SendAsync(message);
            await client.DisconnectAsync(quit: true);
        }
    }
}