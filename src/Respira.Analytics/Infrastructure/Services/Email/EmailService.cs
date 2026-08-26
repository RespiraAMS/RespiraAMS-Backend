using Application.Contracts.Services;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using MimeKit.Text;

namespace Infrastructure.Services.Email
{
    public class EmailService(IOptions<MailServiceOptions> options) : IEmailService
    {
        private const string SystemName = "RespiraAMS";
        public Task async SendEmailAsync(IEnumerable<(string Name, string Email)> recipients, string subject, string body, (string filename, byte[] data)? attachment)
        {
            // Create message
            var message = new MimeMessage();

            // Add message from
            message.From.Add(new MailboxAddress(SystemName, options.Value.Username));

            // Add recipients
            foreach (var (name, email) in recipients)
            {
                message.To.Add(new MailboxAddress(name, email));
            }

            // Add subject
            message.Subject = subject;

            // Add body with text content and attachment
            var builder = new BodyBuilder { HtmlBody = body };
            if (attachment is not null)
            {
                builder.Attachments.Add(attachment.Value.filename, attachment.Value.data);
            }
            message.Body = builder.ToMessageBody();

            // Send message
            using var client = new SmtpClient();
            await client.ConnectAsync(options.Value.Host, options.Value.Port, options.Value.EnableSsl ? SecureSocketOptions.StartTlsWhenAvailable : SecureSocketOptions.None);

            await client.AuthenticateAsync(options.Value.Username, options.Value.Password);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);
        }
    }
}
