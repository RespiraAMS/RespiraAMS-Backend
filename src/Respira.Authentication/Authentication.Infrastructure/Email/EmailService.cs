using Authentication.Application.Constracts.Email;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

namespace Authentication.Infrastructure.Email;

public sealed class EmailService(
    IOptions<EmailOption> emailOption,
    ILogger<EmailService> logger
) : IEmailService
{
    private readonly EmailOption _emailOption = emailOption.Value;

    public async Task SendAsync(
        string recipientEmail,
        string subject,
        string htmlBody,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(recipientEmail);
        ArgumentException.ThrowIfNullOrWhiteSpace(subject);
        ArgumentException.ThrowIfNullOrWhiteSpace(htmlBody);

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_emailOption.FromName, _emailOption.FromEmail));
        message.To.Add(MailboxAddress.Parse(recipientEmail));
        message.Subject = subject;
        message.Body = new BodyBuilder { HtmlBody = htmlBody }.ToMessageBody();

        using var smtpClient = new SmtpClient();
        var secureSocketOptions = _emailOption.UseSsl
            ? SecureSocketOptions.SslOnConnect
            : SecureSocketOptions.StartTlsWhenAvailable;

        await smtpClient.ConnectAsync(
            _emailOption.Host,
            _emailOption.Port,
            secureSocketOptions,
            cancellationToken
        );
        await smtpClient.AuthenticateAsync(
            _emailOption.UserName,
            _emailOption.Password,
            cancellationToken
        );
        await smtpClient.SendAsync(message, cancellationToken);
        await smtpClient.DisconnectAsync(true, cancellationToken);

        logger.LogInformation("Email sent to {RecipientEmail}.", recipientEmail);
    }
}
